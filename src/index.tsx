import 'react-app-polyfill/ie11';
import 'react-app-polyfill/stable';

import React from 'react';
import ReactDOM from 'react-dom';
import { App } from './App';
import { mergeStyles } from 'office-ui-fabric-react';
import { loadTheme } from 'office-ui-fabric-react';
import { initializeIcons } from '@uifabric/icons';
import './style.css'

initializeIcons();

loadTheme({
  palette: {
    themePrimary: '#5fbaff',
    themeLighterAlt: '#04070a',
    themeLighter: '#0f1e29',
    themeLight: '#1c384d',
    themeTertiary: '#396f99',
    themeSecondary: '#53a3e0',
    themeDarkAlt: '#6fc1ff',
    themeDark: '#85caff',
    themeDarker: '#a5d8ff',
    neutralLighterAlt: '#0b0b0b',
    neutralLighter: '#151515',
    neutralLight: '#252525',
    neutralQuaternaryAlt: '#2f2f2f',
    neutralQuaternary: '#373737',
    neutralTertiaryAlt: '#595959',
    neutralTertiary: '#c8c8c8',
    neutralSecondary: '#d0d0d0',
    neutralPrimaryAlt: '#dadada',
    neutralPrimary: '#ffffff',
    neutralDark: '#f4f4f4',
    black: '#f8f8f8',
    white: '#000000',
  }
});

// Inject some global styles
mergeStyles({
  selectors: {
    ':global(body), :global(html), :global(#root)': {
      margin: 0,
      padding: 0,
      height: '100vh',
      background: '#000000',
      color: '#ffffff',
      overflow: 'hidden',
      fontFamily: `-apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', 'Oxygen', 'Ubuntu', 'Cantarell', 'Fira Sans', 'Droid Sans', 'Helvetica Neue', sans-serif`
    }
  }
});

const rootElement = document.getElementById("root");

const app = (
  <App />
)

if (rootElement && rootElement.hasChildNodes()) {
  ReactDOM.hydrate(app, rootElement);
} else {
  ReactDOM.render(app, rootElement);
}
