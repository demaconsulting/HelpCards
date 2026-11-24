---
title: Graph
summary: Draw a graph with at most {{limit}} nodes.
aliases:
  - diagram
tags:
  - authoring
see-also:
  - lib-b:chart
examples:
  - name: render
    kind: command
    language: shell
    content: '{{tool}} render graph.json'
    description: Render a graph file.
---
A graph may have {{limit}} nodes. Lay it out with [[graph/layout]] and run [[#render]].
Charts are described in [[lib-b:chart|the chart card]].
