
import React from "react";
import { connect } from "react-redux";
import { withRouter } from "react-router";
import { withTranslation } from "react-i18next";
import styled from "styled-components";
import { RowContent, Link, Text, Icons, Badge, toastr } from "asc-web-components";
import { constants } from 'asc-web-common';
import { createFile, createFolder, renameFolder, updateFile, setFilter, fetchFiles } from '../../../../../store/files/actions';
import { canWebEdit, isImage, isSound, isVideo, canConvert, getTitleWithoutExst } from '../../../../../store/files/selectors';
import store from "../../../../../store/store";
import EditingWrapperComponent from "./EditingWrapperComponent";

const { FileAction } = constants;

class FilesRowContent extends React.PureComponent {

  constructor(props) {
    super(props);
    let titleWithoutExt = getTitleWithoutExst(props.item);

    if (props.fileAction.id === -1) {
      titleWithoutExt = this.getDefaultName(props.fileAction.extension);
    }

    this.state = {
      itemTitle: titleWithoutExt,
      editingId: props.fileAction.id
      //loading: false
    };
  }

  completeAction = () => {
    //this.setState({ loading: false }, () =>)
    this.props.onEditComplete();
  }

  updateItem = () => {
    const { fileAction, updateFile, renameFolder, item, onLoading } = this.props;

    const { itemTitle } = this.state;
    const originalTitle = getTitleWithoutExst(item);

    onLoading(true);
    if (originalTitle === itemTitle)
      return this.completeAction();

    item.fileExst
      ? updateFile(fileAction.id, itemTitle)
        .then(() => this.completeAction()).finally(() => onLoading(false))
      : renameFolder(fileAction.id, itemTitle)
        .then(() => this.completeAction()).finally(() => onLoading(false));
  };

  createItem = () => {
    const { createFile, createFolder, item, onLoading } = this.props;
    const { itemTitle } = this.state;

    onLoading(true);

    if (itemTitle.trim() === '')
      return this.completeAction();

    !item.fileExst
      ? createFolder(item.parentId, itemTitle)
        .then(() => this.completeAction()).finally(() => onLoading(false))
      : createFile(item.parentId, `${itemTitle}.${item.fileExst}`)
        .then(() => this.completeAction()).finally(() => onLoading(false))
  }

  componentDidUpdate(prevProps) {
    const { fileAction } = this.props;
    if (fileAction) {
      if (fileAction.id !== prevProps.fileAction.id) {
        this.setState({ editingId: fileAction.id })
      }
    }
  }

  renameTitle = e => {
    this.setState({ itemTitle: e.target.value });
  }

  cancelUpdateItem = () => {
    //this.setState({ loading: false });
    this.completeAction();
  }

  onClickUpdateItem = () => {
    (this.props.fileAction.type === FileAction.Create)
      ? this.createItem()
      : this.updateItem();
  }

  onKeyUpUpdateItem = e => {
    if (e.keyCode === 13) {
      (this.props.fileAction.type === FileAction.Create)
        ? this.createItem()
        : this.updateItem();
    }

    if (e.keyCode === 27)
      return this.cancelUpdateItem()
  }

  onFilesClick = () => {
    const { id, fileExst, viewUrl } = this.props.item;
    const { filter, parentFolder, onLoading, onMediaFileClick } = this.props;
    if (!fileExst) {
      onLoading(true);
      const newFilter = filter.clone();
      if (!newFilter.treeFolders.includes(parentFolder.toString())) {
        newFilter.treeFolders.push(parentFolder.toString());
      }

      fetchFiles(id, newFilter, store.dispatch)
        .catch(err => {
          toastr.error("Something went wrong", err);
          onLoading(false);
        })
        .finally(() => onLoading(false));
    } else {
      if (canWebEdit(fileExst)) {
        return window.open(`./doceditor?fileId=${id}`, "_blank");
      }

      const isOpenMedia = isImage(fileExst) || isSound(fileExst) || isVideo(fileExst);

      if (isOpenMedia) {
        onMediaFileClick(id);
        return;
      }

      return window.open(viewUrl, "_blank");
    }
  };

  onMobileRowClick = (e) => {
    if (window.innerWidth > 1024)
      return;

    this.onFilesClick();
  }

  getStatusByDate = () => {
    const { culture, t, item } = this.props;
    const { created, updated, version, fileExst } = item;

    const title = version > 1
      ? t("TitleModified")
      : fileExst
        ? t("TitleUploaded")
        : t("TitleCreated");

    const date = fileExst ? updated : created;
    const dateLabel = new Date(date).toLocaleString(culture);

    return `${title}: ${dateLabel}`;
  };

  getDefaultName = (format) => {
    const { t } = this.props;

    switch (format) {
      case 'docx':
        return t("NewDocument");
      case 'xlsx':
        return t("NewSpreadsheet");
      case 'pptx':
        return t("NewPresentation");
      default:
        return t("NewFolder");
    }
  };

  onShowVersionHistory = (e) => {
    const {settings, history} = this.props;
    const fileId = e.currentTarget.dataset.id;

    history.push(`${settings.homepage}/${fileId}/history`);
  }

  render() {
    const { t, item, fileAction, isLoading, isTrashFolder } = this.props;
    const { itemTitle, editingId/*, loading*/ } = this.state;
    const {
      contentLength,
      updated,
      createdBy,
      fileExst,
      filesCount,
      fileStatus,
      foldersCount,
      id,
      versionGroup
    } = item;

    const SimpleFilesRowContent = styled(RowContent)`
    .badge-ext {
      margin-left: -8px;
      margin-right: 8px;
    }

    .badge {
      margin-right: 8px;
    }

    .badges {
      display: flex;
      align-items: center;
    }

    .share-icon {
      margin-top: -4px;
      padding-right: 8px;
    }
    `;

    const titleWithoutExt = getTitleWithoutExst(item);
    const fileOwner = createdBy && ((this.props.viewer.id === createdBy.id && t("AuthorMe")) || createdBy.displayName);
    const updatedDate = updated && this.getStatusByDate();
    const canEditFile = fileExst && canWebEdit(fileExst);
    const canConvertFile = fileExst && canConvert(fileExst);

    const okIcon = <Icons.CheckIcon
      className='edit-ok-icon'
      size='scale'
      isfill={true}
      color='#A3A9AE'
    />;

    const cancelIcon = <Icons.CrossIcon
      className='edit-cancel-icon'
      size='scale'
      isfill={true}
      color='#A3A9AE'
    />;

    const isEdit = (id === editingId) && (fileExst === fileAction.extension);
    const linkStyles = isTrashFolder ? { noHover: true } : { onClick: this.onFilesClick };

    return isEdit
      ? <EditingWrapperComponent
        isLoading={isLoading}
        itemTitle={itemTitle}
        okIcon={okIcon}
        cancelIcon={cancelIcon}
        renameTitle={this.renameTitle}
        onKeyUpUpdateItem={this.onKeyUpUpdateItem}
        onClickUpdateItem={this.onClickUpdateItem}
        cancelUpdateItem={this.cancelUpdateItem}
      />
      : (
        <SimpleFilesRowContent
          sideColor="#333"
          isFile={fileExst}
          onClick={this.onMobileRowClick}
        >
          <Link
            containerWidth='100%'
            type='page'
            title={titleWithoutExt}
            fontWeight="bold"
            fontSize='15px'
            {...linkStyles}
            color="#333"
            isTextOverflow
          >
            {titleWithoutExt}
          </Link>
          <>
            {fileExst &&
              <div className='badges'>
                <Text
                  className='badge-ext'
                  as="span"
                  color="#A3A9AE"
                  fontSize='15px'
                  fontWeight={600}
                  title={fileExst}
                  truncate={true}
                >
                  {fileExst}
                </Text>
                {canConvertFile &&
                  <Icons.FileActionsConvertIcon
                    className='badge'
                    size='small'
                    isfill={true}
                    color='#A3A9AE'
                  />
                }
                {canEditFile &&
                  <Icons.AccessEditIcon
                    className='badge'
                    size='small'
                    isfill={true}
                    color='#A3A9AE'
                  />
                }
                {fileStatus === 1 &&
                  <Icons.FileActionsConvertEditDocIcon
                    className='badge'
                    size='small'
                    isfill={true}
                    color='#3B72A7'
                  />
                }
                {false &&
                  <Icons.FileActionsLockedIcon
                    className='badge'
                    size='small'
                    isfill={true}
                    color='#3B72A7'
                  />
                }
                {versionGroup > 1 &&
                  <Badge
                    className='badge-version'
                    backgroundColor="#A3A9AE"
                    borderRadius="11px"
                    color="#FFFFFF"
                    fontSize="10px"
                    fontWeight={800}
                    label={`Ver.${versionGroup}`}
                    maxWidth="50px"
                    onClick={this.onShowVersionHistory}
                    padding="0 5px"
                    data-id={id}
                  />
                }
                {fileStatus === 2 &&
                  <Badge
                    className='badge-version'
                    backgroundColor="#ED7309"
                    borderRadius="11px"
                    color="#FFFFFF"
                    fontSize="10px"
                    fontWeight={800}
                    label={`New`}
                    maxWidth="50px"
                    onClick={this.onShowVersionHistory}
                    padding="0 5px"
                    data-id={id}
                  />
                }
              </div>
            }
          </>
          <Text
            containerMinWidth='120px'
            containerWidth='10%'
            as="div"
            color="#333"
            fontSize='12px'
            fontWeight={600}
            title={fileOwner}
            truncate={true}
          >
            {fileOwner}
          </Text>
          <Link
            containerMinWidth='190px'
            containerWidth='15%'
            type='page'
            title={updatedDate}
            fontSize='12px'
            fontWeight={400}
            color="#333"
            onClick={() => { }}
            isTextOverflow={true}
          >
            {updatedDate && updatedDate}
          </Link>
          <Text
            containerMinWidth='90px'
            containerWidth='8%'
            as="div"
            color="#333"
            fontSize='12px'
            fontWeight={600}
            title=''
            truncate={true}
          >
            {fileExst
              ? contentLength
              : `${t("TitleDocuments")}: ${filesCount} / ${t("TitleSubfolders")}: ${foldersCount}`}
          </Text>
        </SimpleFilesRowContent>
      )
  }
};

function mapStateToProps(state) {
  const { filter, fileAction, selectedFolder, treeFolders } = state.files;
  const { settings } = state.auth;
  const indexOfTrash = 3;

  return {
    filter,
    fileAction,
    parentFolder: selectedFolder.id,
    isTrashFolder: treeFolders[indexOfTrash].id === selectedFolder.id,
    settings
  }
}

export default connect(mapStateToProps, { createFile, createFolder, updateFile, renameFolder, setFilter })(
  withRouter(withTranslation()(FilesRowContent))
);