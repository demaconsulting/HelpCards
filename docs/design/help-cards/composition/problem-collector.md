### ProblemCollector Design

![Composition Structure](CompositionView.svg)

#### Purpose

`ProblemCollector` gathers the problems of one build and returns them in the one deterministic display order, so the
order of problems never depends on the order in which steps found them.

#### Data Model

The collector holds a list of problems, each with the registration index of its source. The display order is the
index, the file path (`StringComparer.Ordinal`), the line, and the rule identifier (`StringComparer.Ordinal`);
problems that are equal in all four keep the order in which they were found, because the sort is stable. An identical
problem of the same source that was added twice is kept once.

#### Key Methods

- **`Add(sourceIndex, problem)`** and **`AddRange(sourceIndex, problems)`** collect problems; null is an
  `ArgumentNullException`.
- **`GetOrdered(severity)`** returns the problems, optionally of one severity, without duplicates, in display order.
- **`HasErrors`** says whether an error was collected.

#### Error Handling

Only null arguments throw. The collector is not safe for concurrent use; it belongs to one build.

#### Dependencies

The problem model of the Catalog subsystem (`HelpProblem`, `HelpSeverity`).

#### Callers

`HelpCatalogBuilder` creates one collector per build and asks it for the errors, the informational problems, and all
problems.
