import React from 'react'

import loadable from "@loadable/component";
import { PrerenderedComponent } from "react-prerendered-component";

export const prerenderedLoader = (dynamicImport: any) => {
  const LoadableComponent = loadable(dynamicImport);
  return React.memo(props => (
    // you can use the `.preload()` method from react-loadable or react-imported-component`
    <PrerenderedComponent live={LoadableComponent.load()}>
      <LoadableComponent {...props} />
    </PrerenderedComponent>
  ));
};
