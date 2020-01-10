import React from 'react'
import ReactMarkdown from 'react-markdown'
import { Stack, Text, Link } from 'office-ui-fabric-react';
import memoize from 'promise-memoize'

const renderers = {
  heading: (props: { level: number; children: any }) => {
    const { level, children } = props

    const size: any = {
      1: 'mega',
      2: 'xxLarge',
      3: 'xLarge'
    }

    const variant = size[level]
    const title: any = `h${level}`

    return <Text as={title} variant={variant}>{children[0].props.value}</Text>
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
            maxWidth: 960,
            height: 'auto'
          }
        }}>
        <ReactMarkdown renderers={renderers} source={markdown} />
      </Stack>
    )
  }
}