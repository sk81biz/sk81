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


using ASC.ApiSystem.Classes;
using ASC.ApiSystem.Interfaces;
using ASC.Core.Tenants;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;


namespace ASC.ApiSystem.Models
{
    [DataContract]
    public class TenantModel : IModel
    {
        [DataMember(IsRequired = true)]
        [StringLength(255)]
        public string PortalName { get; set; }

        [DataMember(IsRequired = false)]
        public int? TenantId { get; set; }


        [DataMember(IsRequired = false)]
        [StringLength(255)]
        public string AffiliateId { get; set; }

        [DataMember(IsRequired = false)]
        public string Campaign { get; set; }

        [DataMember(IsRequired = true)]
        [StringLength(255)]
        public string FirstName { get; set; }

        [DataMember(IsRequired = true)]
        [Email]
        [StringLength(255)]
        public string Email { get; set; }

        [DataMember(IsRequired = false)]
        public int Industry { get; set; }

        [DataMember(IsRequired = false)]
        [StringLength(7)]
        public string Language { get; set; }

        [DataMember(IsRequired = true)]
        [StringLength(255)]
        public string LastName { get; set; }

        [DataMember(IsRequired = false)]
        [StringLength(38)]
        public string Module { get; set; }

        [DataMember(IsRequired = false)]
        [StringLength(Web.Core.Utility.PasswordSettings.MaxLength)]
        public string Password { get; set; }

        [DataMember(IsRequired = false)]
        [StringLength(255)]
        public string PartnerId { get; set; }

        [DataMember(IsRequired = false)]
        [StringLength(32)]
        public string Phone { get; set; }

        [DataMember(IsRequired = false)]
        public string RecaptchaResponse { get; set; }

        [DataMember(IsRequired = false)]
        [StringLength(20)]
        public string Region { get; set; }

        [DataMember(IsRequired = false)]
        public TenantStatus Status { get; set; }

        [DataMember(IsRequired = false)]
        public bool SkipWelcome { get; set; }

        [DataMember(IsRequired = false)]
        [StringLength(255)]
        public string TimeZoneName { get; set; }

        [DataMember(IsRequired = false)]
        public bool Spam { get; set; }

        [DataMember(IsRequired = false)]
        public bool Calls { get; set; }

        [DataMember(IsRequired = false)]
        public bool Analytics { get; set; }

        [DataMember(IsRequired = false)]
        public string AppKey { get; set; }
    }
}