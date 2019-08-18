import React, { useState } from 'react';
import { Stack, IconButton, Panel } from 'office-ui-fabric-react';
import { createMarkdownPage } from './components/markdown-loader'
import { Route, Switch, BrowserRouter } from 'react-router-dom';
import { NavArea } from './components/Nav';
import { NotFound } from './components/404'
import { prerenderedLoader } from './components/prerender-loader'
import MediaQuery from 'react-responsive'

const Home = prerenderedLoader(() => createMarkdownPage('README.md'));
const Changelog = prerenderedLoader(() => createMarkdownPage('CHANGELOG.md'));

export const App: React.FunctionComponent = () => {
  const [panelOpen, setPanelOpen] = useState(false)
  return (
    <BrowserRouter basename="/WinIRC/">
      <>
        <IconButton className="hideOnDesktop"
          onClick={() => setPanelOpen(!panelOpen)}
          iconProps={{ iconName: 'GlobalNavButton' }}
          styles={{
            root: {
              height: 48,
              width: 48,
              right: 0,
              position: 'fixed'
            }
          }}
          title="Menu"
          ariaLabel="Menu" />

        <Panel className="hideOnDesktop" isOpen={panelOpen} isLightDismiss={true} headerText="Menu" onDismiss={() => setPanelOpen(false)}>
          <NavArea closeNav={() => setPanelOpen(false)} />
        </Panel>

        <Stack
          horizontal
          verticalFill
          gap={15}
        >
          <NavArea className="hideOnPhone" />
          <Stack
            styles={{
              root: {
                overflow: 'auto',
                padding: 10,
                width: '100%',
                height: '100vh',
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
      </>

    </BrowserRouter >
  );
};
