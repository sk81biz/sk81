import React from "react";
import { connect } from "react-redux";
import PropTypes from "prop-types";
import { withRouter } from "react-router";
import { RequestLoader, Loader } from "asc-web-components";
import { PageLayout, utils, api } from "asc-web-common";
import { withTranslation, I18nextProvider } from "react-i18next";
import i18n from "./i18n";

import {
  ArticleHeaderContent,
  ArticleBodyContent,
  ArticleMainButtonContent
} from "../../Article";
import { SectionHeaderContent, SectionBodyContent } from "./Section";

const { changeLanguage } = utils;

class PureVersionHistory extends React.Component {
  constructor(props) {
    super(props);

    const { files, match } = props;
    const { fileId } = match.params;
    const found = files.filter(f => f.id == fileId);

    this.state = {
      isLoading: false,
      fileId: props.match.params.fileId,
      file: found && found[0],
      versions: null
    };
  }

  componentDidMount() {
    const { match, t } = this.props;
    const { fileId } = match.params;

    //document.title = `${t("GroupAction")} – ${t("People")}`;

    if (fileId) {
      //fetchGroup(fileId);
      api.files.getFileVersionInfo(fileId)
      .then((versions) => {
        console.log("getFileVersionInfo result", versions);
        this.setState({
          versions
        })
      })

      console.log("Loading file versions", fileId);
    }
  }
  
  onLoading = status => {
    this.setState({ isLoading: status });
  };

  render() {
    const { file, versions } = this.state;
    const { t, settings } = this.props;

    console.log(`FileId ${file.id}`);

    return (
      <>
        <RequestLoader
          visible={this.state.isLoading}
          zIndex={256}
          loaderSize="16px"
          loaderColor={"#999"}
          label={`${t("LoadingProcessing")} ${t("LoadingDescription")}`}
          fontSize="12px"
          fontColor={"#999"}
        />
        {versions ? (
          <PageLayout
            withBodyScroll={true}
            withBodyAutoFocus={true}
            articleHeaderContent={<ArticleHeaderContent />}
            articleMainButtonContent={
              <ArticleMainButtonContent
                onLoading={this.onLoading}
                startUpload={this.startUpload}
                setProgressVisible={this.setProgressVisible}
                setProgressValue={this.setProgressValue}
                setProgressLabel={this.setProgressLabel}
              />
            }
            articleBodyContent={
              <ArticleBodyContent
                onLoading={this.onLoading}
                isLoading={this.state.isLoading}
              />
            }
            sectionHeaderContent={<SectionHeaderContent title={file.title} />}
            sectionBodyContent={
              <SectionBodyContent onLoading={this.onLoading} versions={versions} culture={settings.culture} />
            }
          />
        ) : (
          <PageLayout
            articleHeaderContent={<ArticleHeaderContent />}
            articleMainButtonContent={<ArticleMainButtonContent />}
            articleBodyContent={<ArticleBodyContent />}
            sectionBodyContent={
              <Loader className="pageLoader" type="rombs" size="40px" />
            }
          />
        )}
      </>
    );
  }
}

const VersionHistoryContainer = withTranslation()(PureVersionHistory);

const VersionHistory = props => {
  changeLanguage(i18n);
  return (
    <I18nextProvider i18n={i18n}>
      <VersionHistoryContainer {...props} />
    </I18nextProvider>
  );
};

VersionHistory.propTypes = {
  files: PropTypes.array,
  history: PropTypes.object.isRequired,
  isLoaded: PropTypes.bool
};

function mapStateToProps(state) {
  return {
    files: state.files.files,
    settings: state.auth.settings,
    isLoaded: state.auth.isLoaded
  };
}

export default connect(mapStateToProps)(withRouter(VersionHistory));
