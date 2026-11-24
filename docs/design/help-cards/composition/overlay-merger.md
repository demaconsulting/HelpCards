### OverlayMerger Design

![Composition Structure](CompositionView.svg)

#### Purpose

`OverlayMerger` lets a source change the cards of another source without owning them. An overlay is a file with an
`extends` key; the merger finds the card it extends and adds the overlay's text to it. It is the only way a source
contributes to the cards of another source.

#### Data Model

The class is static and stateless. It reads the overlay definitions of each source entry and changes the `CardBuild`
of the target card: the title and summary, and the lists of body segments, see-also entries, examples, and extension
keys. Each added body, see-also entry, and example remembers the source of the overlay, because its references
resolve in that source while the text is shown on the target card.

#### Key Methods

- **`Apply(registry, problems)`** visits the sources in registration order and, inside a source, the overlays by
  ordinal file path, and applies each one.
- **Target resolution.** The `extends` value is `[source:]localId`. A qualified target must name a source that is
  registered and not added after the overlay's source; an absent source makes an optional overlay skip with the
  informational `overlay-optional-skipped` and any other overlay `extends-target`; a source added later is
  `extends-target`. The card is then looked up with `SourceRegistry.Lookup`: a bare id is looked up in the overlay's own
  source and then in its imports. No card is `overlay-target` (a generated mount root has no local id and cannot be
  targeted, which the message says); an id that the source and an import both have is `ambiguous-reference`. An
  optional overlay whose card is missing is still an error.
- **Merging.** The title and summary of the overlay replace those of the target, so the last overlay wins; the body is
  appended as a separate segment (the pipeline puts a blank line between segments and none for an empty body);
  see-also entries and examples are appended; extension keys replace keys of the same name. All problems are reported
  at the file of the overlay and the line of `extends`.

#### Error Handling

A null argument throws `ArgumentNullException`. Defects are problems. A target that failed to load or whose source was
excluded is silent because the cause is already reported.

#### Dependencies

Composition (`SourceRegistry`, `SourceEntry`, `CardBuild`, `ProblemCollector`), Loading (`OverlayDefinition`,
`ReferenceDefinition`), and the problem model of Catalog.

#### Callers

`HelpCatalogBuilder` calls `Apply` in step 6 of the pipeline, after the cards of every source are placed and before the
nodes are created and the references are resolved.
