import React, { memo } from 'react';
import styled from 'styled-components';
import PropTypes from 'prop-types';
import CustomScrollbarsVirtualList from '../scrollbar/custom-scrollbars-virtual-list';
import { FixedSizeList as List, areEqual } from 'react-window';
import AutoSizer from 'react-virtualized-auto-sizer';
import ContextMenu from '../context-menu';

const StyledRowContainer = styled.div`
  height: ${props => props.manualHeight ? props.manualHeight : '100%'};
`;

class RowContainer extends React.PureComponent {
  constructor(props) {
    super(props);

    this.state = {
      contextOptions: []
    };
  };

  onRowContextClick = (options) => {
    if (Array.isArray(options)) {
      this.setState({
        contextOptions: options
      });
    }
  };

  componentDidMount() {
    window.addEventListener('contextmenu', this.onRowContextClick);
  }

  componentWillUnmount() {
    window.removeEventListener('contextmenu', this.onRowContextClick);
  }

  render() {
    const { manualHeight, itemHeight, children } = this.props;

    const renderList = ({ height, width }) => (
      <List
        className="List"
        height={height}
        width={width}
        itemSize={itemHeight}
        itemCount={children.length}
        itemData={children}
        outerElementType={CustomScrollbarsVirtualList}
      >
        {renderRow}
      </List>
    );


    const renderRow = memo(({ data, index, style }) => {

      const options = data[index].props.contextOptions;
      
      return (
        <div onContextMenu={this.onRowContextClick.bind(this, options)} style={style}>
          {data[index]}
        </div>
      )
    },
      areEqual
    );

    return (
      <StyledRowContainer id='rowContainer' manualHeight={manualHeight}>
        <AutoSizer>
          {renderList}
        </AutoSizer>
        <ContextMenu targetAreaId='rowContainer' options={this.state.contextOptions} />
      </StyledRowContainer>
    );
  }
};

RowContainer.propTypes = {
  itemHeight: PropTypes.number,
  manualHeight: PropTypes.string,
  children: PropTypes.any.isRequired
};

RowContainer.defaultProps = {
  itemHeight: 50,
};

export default RowContainer;