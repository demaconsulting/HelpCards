---
title: Check
summary: Check a configuration file.
examples:
  - name: config
    kind: source
    language: yaml
    file: check/config.yaml
    save-as: check/config.yaml
    description: A configuration file that passes the check.
  - name: limits
    kind: source
    language: yaml
    file: check/limits.yaml
    description: A file whose save-as name is the last segment of its file.
  - name: notes
    kind: source
    language: text
    save-as: notes.txt
    content: |
      first line
      second line
    description: A text file written inline.
  - name: ok
    kind: command
    language: shell
    content: '{{tool}} check check/config.yaml'
    expect: |
      Checked 1 file
      No problems
    description: Check the configuration file.
  - name: missing
    kind: command
    language: shell
    content: '{{tool}} check absent.yaml'
    exit: 2
    expect: Cannot open absent.yaml
    description: Check a file that does not exist.
---
Run [[#ok]] on the file of [[#config]], or see [[check/details]] for more.
