---
title: Guide
summary: How to set up and use the tool.
aliases:
  - start
tags:
  - tutorial
  - authoring
order: 10
see-also:
  - reference/limits
  - other:thing
examples:
  - name: run
    kind: command
    language: shell
    content: '{{tool}} run input.txt'
    description: Run the tool on a file.
  - name: settings
    kind: source
    language: yaml
    file: guide/settings.yaml
    description: A settings file with the limit.
  - name: fragment
    kind: snippet
    language: yaml
    file: fragment.yaml
    content: |
      limit: {{limit}}
      mode: fast
    description: A fragment of a settings file.
---
Start with [[guide/setup]] and then read [[guide/usage]].
Run it with [[#run]] or see [[guide#settings]] for the settings file.
The limits are described in [[simple:reference/limits|the limits card]], and
[[other:thing|the thing]] lives in another library.
A code span such as `[[x]]` is not a link.
Version {{version}} of {{tool}} accepts at most {{limit}} items.
