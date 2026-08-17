---
slug: feat--enumerable-estimation-extensions
type: feat
status: open
priority: low
created: 2026-08-17
updated: 2026-08-17
---

# feat: Enumerable estimation and predicate extensions

## Problem
`EnumerableExtensions` in `ActDim.Practix.Common` provides `EstimateCount` overloads but lacks higher-level estimation helpers (`EstimateValue`, `EstimateAggregation`, `EstimateComposition`) and `Any`/`Some` bounded evaluation extensions.

## Acceptance Criteria
- Implement `EstimateValue`, `EstimateAggregation`, `EstimateComposition`, and `EstimateProduct` helpers in `EnumerableExtensions`.
- Implement `Any`/`Some` bounded early-exit evaluation helpers over indexed predicates.
- Add unit tests for all new estimation extension methods.
