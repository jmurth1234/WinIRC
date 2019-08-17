import React from 'react'
import ReactMarkdown from 'react-markdown'
import { Stack, Text, Link } from 'office-ui-fabric-react';
import memoize from 'promise-memoize'

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

const getMarkdown = memoize(async (url: string) => {
  const req = await fetch(url)
  const markdown = await req.text()

  return markdown
})

export const createMarkdownPage = async (filename: String) => {
  const markdown = await getMarkdown('https://raw.githubusercontent.com/rymate1234/WinIRC/master/' + filename)
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