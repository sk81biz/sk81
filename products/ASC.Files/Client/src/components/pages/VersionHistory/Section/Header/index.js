import React from "react";
import styled, { css } from "styled-components";
import { withRouter } from "react-router";
import { Headline } from 'asc-web-common';
import { IconButton } from "asc-web-components";
import { connect } from "react-redux";
import { withTranslation } from "react-i18next";

const StyledContainer = styled.div`

  display: flex;
  align-items: center;

  .arrow-button {
    margin-right: 16px;

    @media (max-width: 1024px) {
      padding: 8px 0 8px 8px;
      margin-left: -8px;
    }
  }

  @media (min-width: 1024px) {
    ${props => props.isHeaderVisible && css`width: calc(100% + 76px);`}
  }

  .group-button-menu-container {
    margin: 0 -16px;
    -webkit-tap-highlight-color: rgba(0, 0, 0, 0);
    
    @media (max-width: 1024px) {
      & > div:first-child {
      position: absolute;
      top: 56px;
      z-index: 180;
      }
    }

    @media (min-width: 1024px) {
      margin: 0 -24px;
    }
  }

  .header-container {
    position: relative;

    display: flex;
    align-items: center;
    max-width: calc(100vw - 32px);

    .action-button {
      margin-left: 16px;

      @media (max-width: 1024px) {
        margin-left: auto;

        & > div:first-child {
          padding: 8px 16px 8px 16px;
          margin-right: -16px;
        }
      }
    }
  }
`;

const SectionHeaderContent = props => {

  const { title } = props;

  const onClickBack = () => {
    const { history, settings } = props;
    history.push(settings.homepage);
  };

  return (
    <StyledContainer isHeaderVisible={true}>
        <IconButton
          iconName="ArrowPathIcon"
          size="17"
          color="#A3A9AE"
          hoverColor="#657077"
          isFill={true}
          onClick={onClickBack}
          className="arrow-button"
        />
        <Headline className='headline-header' type="content" truncate={true}>{title}</Headline>
    </StyledContainer>
  );
};

const mapStateToProps = state => {
  return {
    settings: state.auth.settings,
  };
};

export default connect(
  mapStateToProps
)(withTranslation()(withRouter(SectionHeaderContent)));
