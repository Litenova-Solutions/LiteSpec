// The per-type renderers by record type. A type without a renderer falls back
// to Generic, which shows each prose field as its own section.
import type { Component } from "svelte";
import type { BookRecord } from "$lib/book/types";
import Actor from "./Actor.svelte";
import Aggregate from "./Aggregate.svelte";
import ArchitectureLayer from "./ArchitectureLayer.svelte";
import CheckTier from "./CheckTier.svelte";
import Code from "./Code.svelte";
import Constraint from "./Constraint.svelte";
import Decision from "./Decision.svelte";
import EntryPoint from "./EntryPoint.svelte";
import Event from "./Event.svelte";
import Exception from "./Exception.svelte";
import ExternalSystem from "./ExternalSystem.svelte";
import Feature from "./Feature.svelte";
import Finding from "./Finding.svelte";
import FindingCode from "./FindingCode.svelte";
import Folder from "./Folder.svelte";
import Frontend from "./Frontend.svelte";
import Generic from "./Generic.svelte";
import Goal from "./Goal.svelte";
import IntegrationEvent from "./IntegrationEvent.svelte";
import Job from "./Job.svelte";
import Journey from "./Journey.svelte";
import Layer from "./Layer.svelte";
import Module from "./Module.svelte";
import Notification from "./Notification.svelte";
import Permission from "./Permission.svelte";
import Problem from "./Problem.svelte";
import Product from "./Product.svelte";
import Reaction from "./Reaction.svelte";
import Requirement from "./Requirement.svelte";
import Research from "./Research.svelte";
import Result from "./Result.svelte";
import Role from "./Role.svelte";
import Route from "./Route.svelte";
import Rule from "./Rule.svelte";
import Runbook from "./Runbook.svelte";
import Scenario from "./Scenario.svelte";
import Screen from "./Screen.svelte";
import Setting from "./Setting.svelte";
import Term from "./Term.svelte";
import Test from "./Test.svelte";
import TestClass from "./TestClass.svelte";
import TestProject from "./TestProject.svelte";
import UseCase from "./UseCase.svelte";

export type TypeProps = { id: string; r: BookRecord };

export const TYPES: Record<string, Component<TypeProps>> = {
  product: Product,
  goal: Goal,
  problem: Problem,
  feature: Feature,
  requirement: Requirement,
  decision: Decision,
  constraint: Constraint,
  research: Research,
  journey: Journey,
  actor: Actor,
  module: Module,
  aggregate: Aggregate,
  rule: Rule,
  event: Event,
  "use-case": UseCase,
  scenario: Scenario,
  result: Result,
  reaction: Reaction,
  job: Job,
  notification: Notification,
  setting: Setting,
  screen: Screen,
  role: Role,
  permission: Permission,
  "entry-point": EntryPoint,
  "external-system": ExternalSystem,
  runbook: Runbook,
  exception: Exception,
  term: Term,
  layer: Layer,
  "integration-event": IntegrationEvent,
  "architecture-layer": ArchitectureLayer,
  folder: Folder,
  frontend: Frontend,
  "test-project": TestProject,
  "test-class": TestClass,
  "check-tier": CheckTier,
  "finding-code": FindingCode,
  finding: Finding,
  code: Code,
  route: Route,
  test: Test,
};

export const GenericType = Generic;
