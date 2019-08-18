import React from 'react';
import { Stack, Text, FontWeights } from 'office-ui-fabric-react';

const boldStyle = { root: { fontWeight: FontWeights.semibold } };

export const NotFound: React.FunctionComponent = () => {
  return (
    <Stack
      horizontalAlign="center"
      verticalAlign="center"
      verticalFill
      styles={{
        root: {
          width: '960px',
          margin: '0 auto',
          textAlign: 'center'
        }
      }}
      gap={15}
    >
      <Text as='h1' variant="xxLarge" styles={boldStyle}>
        404
      </Text>
      <Text variant="large">Page not found.</Text>
    </Stack>
  );
};