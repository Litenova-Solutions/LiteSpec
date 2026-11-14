# Cross-Module Coupling Practice

LiteSpec 6.0.0 organizes a modular monolith by module, aggregate and use case, and it lets a use case run another use case inline, publish its own integration events, and read another module through a read port. This research record compares that shape with outside practice and with what one large consumer built under it, read on 14 November 2026. It matters because each allowance looked small on its own, and together they grew shared folders and coupled modules that the rules meant to keep apart.

## Findings

Outside practice keeps coupling inside a slice and out of the space between slices. Jimmy Bogard's 2018 article that named vertical slice architecture states the aim as minimal coupling between slices and maximal coupling inside one. A slice that names another slice's request type is coupled to that slice's reasons to change. The record gives no confidence for this claim.

A module can learn another module's state without asking for it. Martin Fowler's 2017 article on event-driven systems names the pattern event-carried state transfer: a receiver keeps its own copy of the data it needs, filled from the events the owner publishes, and stops calling the owner. Fowler names the cost: the receiver maintains that copy and accepts that it lags the owner. The record gives no confidence for this claim.

A port is a seam the core declares for something outside it. Alistair Cockburn's 2005 article on ports and adapters, also called hexagonal architecture, places a port where the application meets a driver or a driven system, and lets adapters change behind it. A repository and an outside system fit that shape. A reader that one module calls to fetch another module's current state is an in-process service call between two parts of one core, not a seam to the outside. The record gives no confidence for this claim.

A published modular monolith reference separates the two kinds of message. The modular-monolith-with-ddd repository keeps domain events inside the module that raised them, crosses module boundaries with integration events processed in their own transactions, and runs follow-on work inside a module as internal commands from its own processing. No module sends another module's command. The record gives no confidence for this claim.

One large consumer of LiteSpec 5.2.0, with 43 modules and 1,541 use-case folders, measured what the allowances built, before its 6.0.0 upgrade. About 270 files in aggregate `Application/` folders were use-case messages and results moved out of their folders because a second use case named them. 214 files sat in module `Contracts/Commands/` folders, and 104 commands were named outside their module. 101 refusal types sat in `Contracts/Exceptions/`, and other modules threw 72 of them. The record gives no confidence for these counts, which come from one consumer.

The same consumer held 57 inline edges in 47 use-case records, and 33 command handlers held a mediator. Running a command inside another command's transaction needed a nested dispatch scope with two ambient values, a per-message aggregate cache, and reload methods that worked around stale reads. 69 handlers published their own integration events, built from the aggregate rather than from the fact the act raised. The record gives no confidence for these counts.

Cross-module readers were the largest coupling. 86 reader interfaces in `Contracts/Ports/` folders were named in 741 files of other modules: 274 command handlers, 148 query handlers, 114 reactions and the rest in services and steps. The busiest reader had 18 methods, several named for their one caller. A query handler that only shows another module's data is answered better by a screen that calls each owner's query. The record gives no confidence for these counts.

Trigger folders scattered what one use case does. 232 folders named for a fact held event handlers, and 22 facts had such folders in more than one module, so a fact folder never showed every reaction to its fact. Each handler named the command of a use case in another folder, which is why those commands moved to shared folders. The record gives no confidence for these counts.

## Source Notes

The four outside sources are articles and a repository, read through their published text and summaries on 14 November 2026. The consumer counts were taken on the consumer's main branch on the same day and are kept with that consumer. The LiteSpec paths are the 6.0.0 release pages this research changed.
