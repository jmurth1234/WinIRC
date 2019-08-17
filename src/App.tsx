import React, { lazy, Suspense } from 'react';
import { Stack, Shimmer } from 'office-ui-fabric-react';
import { createMarkdownPage } from './components/markdown-loader'
import { Route, Switch, BrowserRouter } from 'react-router-dom';
import { NavArea } from './components/Nav';
import { NotFound } from './components/404'

const Home = lazy(() => createMarkdownPage('README.md'));
const Changelog = lazy(() => createMarkdownPage('CHANGELOG.md'));


const ShimmerArea: React.FunctionComponent = () => (
  <>
    <Shimmer />
    <Shimmer width="75%" />
    <Shimmer width="50%" />
  </>
)

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
          <Suspense fallback={<ShimmerArea />}>
            <Switch>
              <Route path="/" exact component={Home} />
              <Route path="/changelog" component={Changelog} />
              <Route component={NotFound} />
            </Switch>
          </Suspense>
        </Stack>

      </Stack>
    </BrowserRouter>
  );
};
