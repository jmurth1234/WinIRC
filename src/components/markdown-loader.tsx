import React from 'react'
import ReactMarkdown from 'react-markdown'
import { Stack, Text, Link } from 'office-ui-fabric-react';

const renderers = {
  heading: (props: { level: Number; children: React.ReactChildren }) => {
    const { level, children } = props

    const size = {
      1: 'mega',
      2: 'xxLarge',
      3: 'xLarge'
    }

    return <Text as={`h${level}`} variant={size[level]}>{children[0].props.value}</Text>
  },
  text: Text,
  link: Link
}

export const createMarkdownPage = async (markdownText: String) => {
  const req = await fetch('https://raw.githubusercontent.com/rymate1234/WinIRC/master/' + markdownText)
  const markdown = await req.text()

  return {
    default: () => (
      <Stack
        verticalFill
        styles={{
          root: {
            maxWidth: 960
          }
        }}>
        <ReactMarkdown renderers={renderers} source={markdown} />
      </Stack>
    )
  }
}