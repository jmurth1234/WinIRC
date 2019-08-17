import React from 'react';
import { Nav } from 'office-ui-fabric-react';
import { withRouter, RouteComponentProps } from "react-router";

const ver = '1.6.0.0'

const groups = [
  {
    links: [
      {
        name: 'Home',
        url: '/WinIRC/',
      },
      {
        name: 'Changelog',
        url: '/WinIRC/changelog',
      },
      {
        name: 'Github',
        url: 'https://github.com/rymate1234/WinIRC',
      },
      {
        name: 'Store',
        url: 'https://www.microsoft.com/en-us/store/apps/winirc/9nblggh2p0rf',
      },
      {
        name: 'Install App',
        url: `ms-appinstaller:?source=https://rymate1234.github.io/WinIRC/winirc-${ver}.appxbundle`
      },
      {
        name: 'Direct Download',
        url: `https://rymate1234.github.io/WinIRC/winirc-${ver}.appxbundle`
      }
    ]
  }
]

export const NavChild: React.FunctionComponent<RouteComponentProps> = (props) => (
  <Nav
    expandButtonAriaLabel="Expand or collapse"
    selectedAriaLabel="Selected"
    onLinkClick={(evt, item) => {
      if (evt) {
        evt.preventDefault()
      }

      if (item && item.url.includes('http')) {
        window.location.href = item.url
      } else if (item) {
        props.history.push(item.url.replace('/WinIRC', ''))
      }
    }}
    styles={{
      root: {
        width: 200,
        height: '100%',
        boxSizing: 'border-box',
        background: '#111111',
        overflowY: 'auto'
      }
    }}
    groups={groups}
  />
)

export const NavArea = withRouter(NavChild);
