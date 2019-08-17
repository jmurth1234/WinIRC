import React from 'react';
import { Stack } from 'office-ui-fabric-react';
import { createMarkdownPage } from './components/markdown-loader'
import { Route, Switch, BrowserRouter } from 'react-router-dom';
import { NavArea } from './components/Nav';
import { NotFound } from './components/404'
import { prerenderedLoader } from './components/prerender-loader'

const Home = prerenderedLoader(() => createMarkdownPage('README.md'));
const Changelog = prerenderedLoader(() => createMarkdownPage('CHANGELOG.md'));

export const App: React.FunctionComponent = () => {
  return (
    <BrowserRouter basename="/WinIRC/">
      <Stack
        horizontal
        verticalFill
        gap={15}
      >
        <NavArea />
        <Stack
          styles={{
            root: {
              overflow: 'auto',
              padding: 10,
              width: '100%'
            }
          }}
          gap={15}
        >
          <Switch>
            <Route path="/" exact component={Home} />
            <Route path="/changelog" component={Changelog} />
            <Route component={NotFound} />
          </Switch>
        </Stack>

      </Stack>
    </BrowserRouter>
  );
};
