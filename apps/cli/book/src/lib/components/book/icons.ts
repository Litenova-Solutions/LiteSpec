import type { Component } from "svelte";
import {
  Activity, ArrowRight, ArrowUp, Ban, Bell, BookA, BookOpen, Boxes, ChevronLeft, ChevronRight, ChevronsDownUp, ChevronsUpDown,
  Circle, CircleAlert, CircleCheck, CircleCheckBig, CircleDashed, CircleDot, CircleQuestionMark, CircleSlash, CircleX, Clock, Clock3,
  Code, Copy, DoorOpen, Download, ExternalLink, Fence, FileCheck, FlaskConical, Folder, Funnel, Gauge, Globe, Hourglass, IdCard, Info,
  KeyRound, Keyboard, Layers, Link, ListChecks, ListTree, LocateFixed, Lock, Microscope, Minus, Monitor, Moon, Network, Package,
  PencilLine, Plug, Plus, Quote, Radio, Route, Scale, Search, Server, Settings, Shield, ShieldOff, Signpost, Sun, Table2, Target,
  TextAlignStart, Timer, TriangleAlert, User, Vote, Waypoints, Workflow, X, Zap,
} from "@lucide/svelte";

// One icon per record type and per mark. A record type the table does not name
// reads with a plain circle, never a guessed icon.
export const ICONS: Record<string, Component> = {
  product: Package, problem: TriangleAlert, goal: Target, feature: Boxes, requirement: FileCheck, decision: Scale,
  constraint: Fence, research: Microscope, journey: Route, topic: TextAlignStart, actor: User, module: Folder,
  aggregate: Table2, rule: Shield, event: Activity, "integration-event": Radio, "use-case": Zap, scenario: ListChecks,
  result: CircleDot, reaction: Workflow, job: Timer, notification: Bell, setting: Settings, screen: Monitor,
  role: IdCard, permission: KeyRound, "entry-point": DoorOpen, "external-system": Plug, limit: Gauge, runbook: BookOpen,
  exception: ShieldOff, term: BookA, code: Code, test: FlaskConical, route: Signpost, layer: Layers,
  // layers and marks
  core: Layers, folder: Folder, shield: Shield, server: Server, book: BookOpen, chev: ChevronRight, sun: Sun, moon: Moon,
  download: Download, approved: CircleCheck, mine: CircleCheckBig, draft: Clock3, inherited: CircleDashed,
  question: CircleQuestionMark, changes: PencilLine, link: Link, flask: FlaskConical, passing: CircleCheck, failing: CircleX,
  awaiting: Hourglass, unconfirmed: CircleDot, unproved: CircleSlash, x: X, plus: Plus, minus: Minus, arrow: ArrowRight,
  up: ArrowUp, back: ChevronLeft, ext: ExternalLink, alert: TriangleAlert, info: Info, clock: Clock, quote: Quote, lock: Lock,
  globe: Globe, missing: CircleAlert, tree: ListTree, expand: ChevronsUpDown, collapse: ChevronsDownUp, ballot: Vote,
  copy: Copy, filter: Funnel, search: Search, keyboard: Keyboard, locate: LocateFixed, refused: Ban, trace: Waypoints,
  network: Network,
};

export const LAYER_ICON: Record<string, string> = {
  intent: "core", behavior: "folder", implementation: "code", proof: "flask", governance: "exception", glossary: "term",
};

export function iconFor(name: string): Component { return ICONS[name] ?? Circle; }
