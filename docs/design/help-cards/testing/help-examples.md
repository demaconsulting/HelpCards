### HelpExamples Design

![Testing Structure](TestingView.svg)

#### Purpose

`HelpExamples` lists the examples of a catalog together with their cards so that a product can test every example
with theory-style tests: run each command with its own entry point, compile each source, or compare each snippet with
its guide. The library never runs an example.

#### Data Model

The class is static and has no state. `Enumerate` returns tuples of the card (`HelpCard`) and one of its examples
(`HelpExample`).

#### Key Methods

- **`Enumerate(catalog, kinds)`** checks its arguments at the call, copies the requested kinds into a set (a null
  kind is an `ArgumentNullException`), and returns the examples of the visible cards in catalog order and in the order
  of each card, filtered by exact kind. No kinds means every kind; an empty list of kinds means none.

#### Error Handling

A null catalog or a null kind throws `ArgumentNullException` when the method is called, not when the sequence is
read.

#### Dependencies

`HelpCatalog` (its cards) and `HelpCard` (its examples) of the Catalog subsystem.

#### Callers

Adopters in their test projects, and the tests of this repository.
