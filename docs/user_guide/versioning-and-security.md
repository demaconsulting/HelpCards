# Versioning, Compatibility, and Security

## Releases and the Public Surface

The first release is `0.1.0-beta.1`. The public surface is the set of public types of the namespaces `Loading`,
`Composition`, `Catalog`, `Presentation`, `Integration`, and `Testing` below `DemaConsulting.HelpCards`; everything else
is internal and can change in any release. The surface is frozen at 1.0 after the first adopters ship: from then on
members are only added, never removed or changed. `HelpSource` and `HelpCatalogBuilder` use only types of the Base Class
Library in their signatures, so exposing a source never forces another package on your callers.

## Card Format Compatibility

The `schema` key of `_source.md` is the major version of the card format. Omitting it means schema 1.

- HelpCards reads schema 1 and schema 2. Schema 2 (introduced in 0.1.0-beta.3) separates the key `save-as`, the name
  to save an example as, from the key `file`, a companion data file; in schema 1 the key `file` meant both. Use schema 2
  for new sources. Schema 1 is supported by every 0.x and 1.x release: a loader supports every schema ever released
  until a major release removes one, after at least two minor releases that announced the removal in the release
  notes, and no removal is planned.
- To move a source to schema 2, add `schema: 2` to its `_source.md`; in each example that has `file` and `content`
  rename `file` to `save-as`; an example with only `file` keeps working but is now saved under the last segment of
  `file` (add `save-as` with the old path to keep its folders). Sources that import or mount each other may use
  different schemas.
- A source with a schema newer than the loader's maximum fails with the rule `schema-unsupported`; the message names the
  source, its schema, and the highest supported schema.
- New keys that add to the format never change the schema; a change of what an existing key means does (as `save-as` and
  `file` did). An unknown key without the prefix `x-` is an error inside a schema, so a library
  that uses a key introduced in a later release must reference at least that release of HelpCards in its package
  reference. The README lists the release that introduced each key.

## JSON Compatibility

The JSON documents carry `helpSchemaVersion`, now 1. Within version 1, new keys appear only at the end of an object
and keys are never removed or reordered. A reader should ignore keys it does not know. The documents of the command-line
helper and of the catalog are the same contract.

## Output Compatibility

Rendered text is byte-identical across machines, cultures, and operating systems: LF line endings, no trailing line
feed, no color codes, and no dependence on the current culture. A tool that needs its own output format, such as an
existing JSON envelope, keeps it with `HelpRenderOptions.JsonEnvelope` and a `LinkFormatter` instead of re-implementing
the rendering.

## Compatibility with Earlier Help Engines

A library that has its own card loader can move to HelpCards one source at a time:

- Keep the resource names. The embedding targets use the same logical names as an embedded `Help` folder, so a library
  that already embeds `{Assembly}.Help/...` needs no change in names.
- Move the examples next to their cards and write `file:` relative to the card.
- Expose the cards as a `HelpSource`, add the tokens with `WithTokens`, and declare a related library with `Imports`.
- Replace tests that parse cards with `HelpCardValidator.AssertValid` and the example helpers.

## Security Notes

- **Cards are trusted content.** They are written and reviewed by the developers of the library and are embedded in its
  assembly. HelpCards does not execute anything that a card contains; an example is data.
- **Agent arguments are untrusted.** The agent tool treats them as text to search for and never as a path. The
  catalog performs no file input or output after it is created.
- **No public directory source.** Sources come from embedded resources or memory. The only disk access is
  `HelpCardValidator.CompareToDirectory` (a packaging check, not a unit test), which lists the file names below the
  folder you give it (it never reads their content); pass it the folder of your own repository and never a path from the
  input of a user.
- **Example file names cannot leave the card's folder.** The names allow letters, digits, dots, hyphens, and
  underscores with `/` between folders, and forbid `..`, a leading slash, and a drive.
- **No dependencies.** The library uses only the Base Class Library, so it adds nothing to the supply chain of an
  adopter beyond its own package.
