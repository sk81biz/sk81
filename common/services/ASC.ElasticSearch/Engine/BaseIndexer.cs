/*
 *
 * (c) Copyright Ascensio System Limited 2010-2018
 *
 * This program is freeware. You can redistribute it and/or modify it under the terms of the GNU 
 * General Public License (GPL) version 3 as published by the Free Software Foundation (https://www.gnu.org/copyleft/gpl.html). 
 * In accordance with Section 7(a) of the GNU GPL its Section 15 shall be amended to the effect that 
 * Ascensio System SIA expressly excludes the warranty of non-infringement of any third-party rights.
 *
 * THIS PROGRAM IS DISTRIBUTED WITHOUT ANY WARRANTY; WITHOUT EVEN THE IMPLIED WARRANTY OF MERCHANTABILITY OR
 * FITNESS FOR A PARTICULAR PURPOSE. For more details, see GNU GPL at https://www.gnu.org/copyleft/gpl.html
 *
 * You can contact Ascensio System SIA by email at sales@onlyoffice.com
 *
 * The interactive user interfaces in modified source and object code versions of ONLYOFFICE must display 
 * Appropriate Legal Notices, as required under Section 5 of the GNU GPL version 3.
 *
 * Pursuant to Section 7 § 3(b) of the GNU GPL you must retain the original ONLYOFFICE logo which contains 
 * relevant author attributions when distributing the software. If the display of the logo in its graphic 
 * form is not reasonably feasible for technical reasons, you must include the words "Powered by ONLYOFFICE" 
 * in every copy of the program you distribute. 
 * Pursuant to Section 7 § 3(e) we decline to grant you any rights under trademark law for use of our trademarks.
 *
*/


using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using ASC.Common;
using ASC.Common.Caching;
using ASC.Common.Logging;
using ASC.Core;
using ASC.Core.Common.EF;
using ASC.Core.Common.EF.Context;
using ASC.Core.Common.EF.Model;
using ASC.ElasticSearch.Core;
using ASC.ElasticSearch.Service;

using Autofac;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using Nest;

namespace ASC.ElasticSearch
{
    public class BaseIndexerHelper
    {
        public ConcurrentDictionary<string, bool> IsExist { get; set; }
        private readonly ICacheNotify<ClearIndexAction> Notify;

        public BaseIndexerHelper(ICacheNotify<ClearIndexAction> cacheNotify)
        {
            IsExist = new ConcurrentDictionary<string, bool>();
            Notify = cacheNotify;
            Notify.Subscribe((a) =>
            {
                IsExist.AddOrUpdate(a.Id, false, (q, w) => false);
            }, CacheNotifyAction.Any);
        }

        public void Clear<T>(T t) where T : class, ISearchItem
        {
            Notify.Publish(new ClearIndexAction() { Id = t.IndexName }, CacheNotifyAction.Any);
        }
    }

    public class BaseIndexer<T> where T : class, ISearchItem
    {
        private static readonly object Locker = new object();

        protected internal T Wrapper { get { return ServiceProvider.GetService<T>(); } }

        public string IndexName { get { return Wrapper.IndexName; } }

        private const int QueryLimit = 1000;

        public bool IsExist { get; set; }
        public Client Client { get; }
        public ILog Log { get; }
        public TenantManager TenantManager { get; }
        public SearchSettingsHelper SearchSettingsHelper { get; }
        public BaseIndexerHelper BaseIndexerHelper { get; }
        public Settings Settings { get; }
        public IServiceProvider ServiceProvider { get; }
        public WebstudioDbContext WebstudioDbContext { get; }

        public BaseIndexer(
            Client client,
            IOptionsMonitor<ILog> log,
            DbContextManager<WebstudioDbContext> dbContextManager,
            TenantManager tenantManager,
            SearchSettingsHelper searchSettingsHelper,
            BaseIndexerHelper baseIndexerHelper,
            Settings settings,
            IServiceProvider serviceProvider)
        {
            Client = client;
            Log = log.CurrentValue;
            TenantManager = tenantManager;
            SearchSettingsHelper = searchSettingsHelper;
            BaseIndexerHelper = baseIndexerHelper;
            Settings = settings;
            ServiceProvider = serviceProvider;
            WebstudioDbContext = dbContextManager.Value;
        }

        internal void Index(T data, bool immediately = true)
        {
            CreateIfNotExist(data);
            Client.Instance.Index(data, idx => GetMeta(idx, data, immediately));
        }

        internal void Index(List<T> data, bool immediately = true)
        {
            if (!data.Any()) return;

            CreateIfNotExist(data[0]);
            if (data[0] is ISearchItemDocument)
            {
                var currentLength = 0L;
                var portion = new List<T>();
                var portionStart = 0;

                for (var i = 0; i < data.Count; i++)
                {
                    var t = data[i];
                    var runBulk = i == data.Count - 1;

                    if (!(t is ISearchItemDocument wwd) || wwd.Document == null || string.IsNullOrEmpty(wwd.Document.Data))
                    {
                        portion.Add(t);
                    }
                    else
                    {
                        var dLength = wwd.Document.Data.Length;
                        if (dLength >= Settings.MemoryLimit)
                        {
                            try
                            {
                                Index(t, immediately);
                            }
                            catch (Exception e)
                            {
                                Log.Error(e);
                            }

                            wwd.Document.Data = null;
                            wwd.Document = null;
                            GC.Collect();
                            continue;
                        }

                        if (currentLength + dLength < Settings.MemoryLimit)
                        {
                            portion.Add(t);
                            currentLength += dLength;
                        }
                        else
                        {
                            runBulk = true;
                            i--;
                        }
                    }

                    if (runBulk)
                    {
                        var portion1 = portion;
                        Client.Instance.Bulk(r => r.IndexMany(portion1, GetMeta));
                        for (var j = portionStart; j < i; j++)
                        {
                            if (data[j] is ISearchItemDocument doc && doc.Document != null)
                            {
                                doc.Document.Data = null;
                                doc.Document = null;
                            }
                        }

                        portionStart = i;
                        portion = new List<T>();
                        currentLength = 0L;
                        GC.Collect();
                    }
                }
            }
            else
            {
                Client.Instance.Bulk(r => r.IndexMany(data, GetMeta));
            }
        }

        internal void Update(T data, bool immediately = true, params Expression<Func<T, object>>[] fields)
        {
            CreateIfNotExist(data);
            Client.Instance.Update(DocumentPath<T>.Id(data), r => GetMetaForUpdate(r, data, immediately, fields));
        }

        internal void Update(T data, UpdateAction action, Expression<Func<T, IList>> fields, bool immediately = true)
        {
            CreateIfNotExist(data);
            Client.Instance.Update(DocumentPath<T>.Id(data), r => GetMetaForUpdate(r, data, action, fields, immediately));
        }

        internal void Update(T data, Expression<Func<Selector<T>, Selector<T>>> expression, int tenantId, bool immediately = true, params Expression<Func<T, object>>[] fields)
        {
            CreateIfNotExist(data);
            Client.Instance.UpdateByQuery(GetDescriptorForUpdate(data, expression, tenantId, immediately, fields));
        }

        internal void Update(T data, Expression<Func<Selector<T>, Selector<T>>> expression, int tenantId, UpdateAction action, Expression<Func<T, IList>> fields, bool immediately = true)
        {
            CreateIfNotExist(data);
            Client.Instance.UpdateByQuery(GetDescriptorForUpdate(data, expression, tenantId, action, fields, immediately));
        }

        internal void Delete(T data, bool immediately = true)
        {
            Client.Instance.Delete<T>(data, r => GetMetaForDelete(r, immediately));
        }

        internal void Delete(Expression<Func<Selector<T>, Selector<T>>> expression, int tenantId, bool immediately = true)
        {
            Client.Instance.DeleteByQuery(GetDescriptorForDelete(expression, tenantId, immediately));
        }

        public void Flush()
        {
            Client.Instance.Indices.Flush(new FlushRequest(IndexName));
        }

        public void Refresh()
        {
            Client.Instance.Indices.Refresh(new RefreshRequest(IndexName));
        }

        internal bool CheckExist(T data)
        {
            try
            {
                var isExist = BaseIndexerHelper.IsExist.GetOrAdd(data.IndexName, (k) => Client.Instance.Indices.Exists(k).Exists);
                if (isExist) return true;

                lock (Locker)
                {
                    if (isExist) return true;

                    isExist = Client.Instance.Indices.Exists(data.IndexName).Exists;

                    _ = BaseIndexerHelper.IsExist.TryUpdate(data.IndexName, IsExist, false);

                    if (isExist) return true;
                }
            }
            catch (Exception e)
            {
                Log.Error("CheckExist " + data.IndexName, e);
            }
            return false;
        }

        public async Task ReIndex()
        {
            Clear();
            //((IIndexer) this).IndexAll();
        }

        private void Clear()
        {
            var index = WebstudioDbContext.WebstudioIndex.Where(r => r.IndexName == Wrapper.IndexName).FirstOrDefault();

            if (index != null)
            {
                WebstudioDbContext.WebstudioIndex.Remove(index);
            }

            WebstudioDbContext.SaveChanges();

            Log.DebugFormat("Delete {0}", Wrapper.IndexName);
            Client.Instance.Indices.Delete(Wrapper.IndexName);
            BaseIndexerHelper.Clear(Wrapper);
            CreateIfNotExist(Wrapper);
        }

        internal IReadOnlyCollection<T> Select(Expression<Func<Selector<T>, Selector<T>>> expression, bool onlyId = false)
        {
            var func = expression.Compile();
            var selector = ServiceProvider.GetService<Selector<T>>();
            var descriptor = func(selector).Where(r => r.TenantId, TenantManager.GetCurrentTenant().TenantId);
            return Client.Instance.Search(descriptor.GetDescriptor(this, onlyId)).Documents;
        }

        internal IReadOnlyCollection<T> Select(Expression<Func<Selector<T>, Selector<T>>> expression, bool onlyId, out long total)
        {
            var func = expression.Compile();
            var selector = ServiceProvider.GetService<Selector<T>>();
            var descriptor = func(selector).Where(r => r.TenantId, TenantManager.GetCurrentTenant().TenantId);
            var result = Client.Instance.Search(descriptor.GetDescriptor(this, onlyId));
            total = result.Total;
            return result.Documents;
        }

        public void CreateIfNotExist(T data)
        {
            try
            {
                if (CheckExist(data)) return;

                lock (Locker)
                {
                    IPromise<IAnalyzers> analyzers(AnalyzersDescriptor b)
                    {
                        foreach (var c in Enum.GetNames(typeof(Analyzer)))
                        {
                            var c1 = c;
                            b.Custom(c1 + "custom", ca => ca.Tokenizer(c1).Filters(Filter.lowercase.ToString()).CharFilters(CharFilter.io.ToString()));
                        }

                        foreach (var c in Enum.GetNames(typeof(CharFilter)))
                        {
                            if (c == CharFilter.io.ToString()) continue;

                            var charFilters = new List<string>() { CharFilter.io.ToString(), c };
                            var c1 = c;
                            b.Custom(c1 + "custom", ca => ca.Tokenizer(Analyzer.whitespace.ToString()).Filters(Filter.lowercase.ToString()).CharFilters(charFilters));
                        }

                        if (data is ISearchItemDocument)
                        {
                            b.Custom("document", ca => ca.Tokenizer(Analyzer.whitespace.ToString()).Filters(Filter.lowercase.ToString()).CharFilters(CharFilter.io.ToString()));
                        }

                        return b;
                    }

                    var createIndexResponse = Client.Instance.Indices.Create(data.IndexName,
                        c =>
                        c.Map<T>(m => m.AutoMap())
                        .Settings(r => r.Analysis(a =>
                                        a.Analyzers(analyzers)
                                        .CharFilters(d => d.HtmlStrip(CharFilter.html.ToString())
                                        .Mapping(CharFilter.io.ToString(), m => m.Mappings("ё => е", "Ё => Е"))))));

                    IsExist = true;
                }
            }
            catch (Exception e)
            {
                Log.Error("CreateIfNotExist", e);
            }
        }

        private IIndexRequest<T> GetMeta(IndexDescriptor<T> request, T data, bool immediately = true)
        {
            var result = request.Index(data.IndexName).Id(data.Id);

            if (immediately)
            {
                result.Refresh(Elasticsearch.Net.Refresh.True);
            }

            if (data is ISearchItemDocument)
            {
                result.Pipeline("attachments");
            }

            return result;
        }
        private IBulkIndexOperation<T> GetMeta(BulkIndexDescriptor<T> desc, T data)
        {
            var result = desc.Index(IndexName).Id(data.Id);

            if (data is ISearchItemDocument)
            {
                result.Pipeline("attachments");
            }

            return result;
        }

        private IUpdateRequest<T, T> GetMetaForUpdate(UpdateDescriptor<T, T> request, T data, bool immediately = true, params Expression<Func<T, object>>[] fields)
        {
            var result = request.Index(IndexName);

            if (fields.Any())
            {
                result.Script(GetScriptUpdateByQuery(data, fields));
            }
            else
            {
                result.Doc(data);
            }

            if (immediately)
            {
                result.Refresh(Elasticsearch.Net.Refresh.True);
            }

            return result;
        }

        private Func<ScriptDescriptor, IScript> GetScriptUpdateByQuery(T data, params Expression<Func<T, object>>[] fields)
        {
            var source = new StringBuilder();
            var parameters = new Dictionary<string, object>();

            for (var i = 0; i < fields.Length; i++)
            {
                var func = fields[i].Compile();
                var newValue = func(data);
                string name;

                var expression = fields[i].Body;
                var isList = expression.Type.IsGenericType && expression.Type.GetGenericTypeDefinition() == typeof(List<>);


                var sourceExprText = "";

                while (!string.IsNullOrEmpty(name = TryGetName(expression, out var member)))
                {
                    sourceExprText = "." + name + sourceExprText;
                    expression = member.Expression;
                }

                if (isList)
                {
                    UpdateByAction(UpdateAction.Add, (IList)newValue, sourceExprText, parameters, source);
                }
                else
                {
                    if (newValue == default(T))
                    {
                        source.AppendFormat("ctx._source.remove('{0}');", sourceExprText.Substring(1));
                    }
                    else
                    {
                        var pkey = "p" + sourceExprText.Replace(".", "");
                        source.AppendFormat("ctx._source{0} = params.{1};", sourceExprText, pkey);
                        parameters.Add(pkey, newValue);
                    }
                }
            }

            var sourceData = source.ToString();

            return r => r.Source(sourceData).Params(parameters);
        }

        private IUpdateRequest<T, T> GetMetaForUpdate(UpdateDescriptor<T, T> request, T data, UpdateAction action, Expression<Func<T, IList>> fields, bool immediately = true)
        {
            var result = request.Index(IndexName).Script(GetScriptForUpdate(data, action, fields));

            if (immediately)
            {
                result.Refresh(Elasticsearch.Net.Refresh.True);
            }

            return result;
        }

        private Func<ScriptDescriptor, IScript> GetScriptForUpdate(T data, UpdateAction action, Expression<Func<T, IList>> fields)
        {
            var source = new StringBuilder();

            var func = fields.Compile();
            var newValue = func(data);
            string name;

            var expression = fields.Body;

            MemberExpression member;
            var sourceExprText = "";

            while (!string.IsNullOrEmpty(name = TryGetName(expression, out member)))
            {
                sourceExprText = "." + name + sourceExprText;
                expression = member.Expression;
            }

            var parameters = new Dictionary<string, object>();

            UpdateByAction(action, newValue, sourceExprText, parameters, source);

            return r => r.Source(source.ToString()).Params(parameters);
        }

        private void UpdateByAction(UpdateAction action, IList newValue, string key, Dictionary<string, object> parameters, StringBuilder source)
        {
            var paramKey = "p" + key.Replace(".", "");
            switch (action)
            {
                case UpdateAction.Add:
                    for (var i = 0; i < newValue.Count; i++)
                    {
                        parameters.Add(paramKey + i, newValue[i]);
                        source.AppendFormat("if (!ctx._source{0}.contains(params.{1})){{ctx._source{0}.add(params.{1})}}", key, paramKey + i);
                    }
                    break;
                case UpdateAction.Replace:
                    parameters.Add(paramKey, newValue);
                    source.AppendFormat("ctx._source{0} = params.{1};", key, paramKey);
                    break;
                case UpdateAction.Remove:
                    for (var i = 0; i < newValue.Count; i++)
                    {
                        parameters.Add(paramKey + i, newValue[i]);
                        source.AppendFormat("ctx._source{0}.removeIf(item -> item.id == params.{1}.id)", key, paramKey + i);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException("action", action, null);
            }
        }

        private string TryGetName(Expression expr, out MemberExpression member)
        {
            member = expr as MemberExpression;
            if (member == null)
            {
                var unary = expr as UnaryExpression;
                if (unary != null)
                {
                    member = unary.Operand as MemberExpression;
                }
            }

            return member == null ? "" : member.Member.Name.ToLowerCamelCase();
        }

        private IDeleteRequest GetMetaForDelete(DeleteDescriptor<T> request, bool immediately = true)
        {
            var result = request.Index(IndexName);
            if (immediately)
            {
                result.Refresh(Elasticsearch.Net.Refresh.True);
            }
            return result;
        }

        private Func<DeleteByQueryDescriptor<T>, IDeleteByQueryRequest> GetDescriptorForDelete(Expression<Func<Selector<T>, Selector<T>>> expression, int tenantId, bool immediately = true)
        {
            var func = expression.Compile();
            var selector = ServiceProvider.GetService<Selector<T>>();
            var descriptor = func(selector).Where(r => r.TenantId, tenantId);
            return descriptor.GetDescriptorForDelete(this, immediately);
        }

        private Func<UpdateByQueryDescriptor<T>, IUpdateByQueryRequest> GetDescriptorForUpdate(T data, Expression<Func<Selector<T>, Selector<T>>> expression, int tenantId, bool immediately = true, params Expression<Func<T, object>>[] fields)
        {
            var func = expression.Compile();
            var selector = ServiceProvider.GetService<Selector<T>>();
            var descriptor = func(selector).Where(r => r.TenantId, tenantId);
            return descriptor.GetDescriptorForUpdate(this, GetScriptUpdateByQuery(data, fields), immediately);
        }

        private Func<UpdateByQueryDescriptor<T>, IUpdateByQueryRequest> GetDescriptorForUpdate(T data, Expression<Func<Selector<T>, Selector<T>>> expression, int tenantId, UpdateAction action, Expression<Func<T, IList>> fields, bool immediately = true)
        {
            var func = expression.Compile();
            var selector = ServiceProvider.GetService<Selector<T>>();
            var descriptor = func(selector).Where(r => r.TenantId, tenantId);
            return descriptor.GetDescriptorForUpdate(this, GetScriptForUpdate(data, action, fields), immediately);
        }

        public IEnumerable<List<T>> IndexAll(Func<DateTime, (int, int, int)> getCount, Func<long, long, DateTime, List<T>> getData)
        {
            var lastIndexed = WebstudioDbContext.WebstudioIndex
                .Where(r => r.IndexName == Wrapper.IndexName)
                .Select(r => r.LastModified)
                .FirstOrDefault();

            var (count, max, min) = getCount(lastIndexed);
            Log.Debug($"Index: {IndexName}, Count {count}, Max: {max}, Min: {min}");

            if (count != 0)
            {
                var step = (max - min + 1) / count;

                if (step == 0)
                {
                    step = 1;
                }

                if (step < QueryLimit)
                {
                    step = QueryLimit;
                }

                for (var i = min; i <= max; i += step)
                {
                    yield return getData(i, step, lastIndexed);
                    //FactoryIndexer<T>.Index(data.Cast<T>().ToList());
                }
            }


            WebstudioDbContext.AddOrUpdate(r => r.WebstudioIndex, new DbWebstudioIndex()
            {
                IndexName = Wrapper.IndexName,
                LastModified = DateTime.UtcNow
            });

            WebstudioDbContext.SaveChanges();

            Log.Debug($"index completed {Wrapper.IndexName}");
        }
    }

    static class CamelCaseExtension
    {
        internal static string ToLowerCamelCase(this string str)
        {
            return str.ToLowerInvariant()[0] + str.Substring(1);
        }
    }


    public enum UpdateAction
    {
        Add,
        Replace,
        Remove
    }

    public static class BaseIndexerExtention
    {
        public static DIHelper AddBaseIndexerHelperService(this DIHelper services)
        {
            services.TryAddSingleton<BaseIndexerHelper>();
            return services.AddKafkaService();
        }

        public static DIHelper AddBaseIndexerService<T>(this DIHelper services) where T : class, ISearchItem
        {
            services.TryAddScoped<BaseIndexer<T>>();

            return services
                .AddFactoryIndexerService()
                .AddClientService()
                .AddWebstudioDbContextService()
                .AddTenantManagerService()
                .AddSearchSettingsHelperService()
                .AddBaseIndexerHelperService()
                ;
        }
    }
}