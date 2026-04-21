# LayerBase Performance Release Notes

> Benchmark focus for this release:
> 1. **Cross-framework Notify comparison** between **C# event**, **LayerBase**, and **MessagePipe**.
> 2. **Internal LayerBase comparison** between **Sync** and **Notify** dispatch under the same fanout levels.

## Executive Summary

- **Single-subscriber Notify** is no longer a major weakness for LayerBase. In the current compare suite, LayerBase is effectively in the same performance class as MessagePipe.
- **4 subscribers** is still the weakest comparison point for LayerBase versus MessagePipe, but LayerBase is already much faster than native C# event here.
- From **16 subscribers onward**, LayerBase clearly overtakes MessagePipe and widens the gap as fanout grows.
- Inside LayerBase itself, **Notify stays consistently faster than Sync** across every tested fanout level.

## 1. Cross-Framework: Single-Subscriber Notify

![Notify Single-Subscriber Comparison](release_single_subscriber_bar.png)

| Framework       | Mean (us)   |
|:----------------|:------------|
| Direct Delegate | 251.6       |
| C# event        | 350.2       |
| LayerBase       | 1,796.2     |
| MessagePipe     | 1,887.1     |

## 2. Cross-Framework: Notify Fanout Raw Data

![Notify Fanout Comparison - Linear](release_notify_fanout_bar_linear.png)

![Notify Fanout Comparison - Log](release_notify_fanout_bar_log.png)

|   Subscriber Count | C# event (us)   | LayerBase (us)   | MessagePipe (us)   |
|-------------------:|:----------------|:-----------------|:-------------------|
|                  1 | 378.3           | 1,804.0          | 1,940.8            |
|                  4 | 11,377.4        | 3,678.7          | 2,996.9            |
|                 16 | 37,986.3        | 6,340.8          | 9,903.6            |
|                 64 | 149,999.3       | 17,630.7         | 34,953.8           |

## 3. Cross-Framework: Analysis Table

Interpretation:
- **LayerBase vs C# event**: positive means LayerBase is faster; negative means slower.
- **LayerBase vs MessagePipe**: positive means LayerBase is faster; negative means slower.
- **/ call (ns)** is the average publish cost per individual send call.

|   Subscriber Count | Winner      | LayerBase vs C# event   | LayerBase vs MessagePipe   |   LayerBase / call (ns) |   MessagePipe / call (ns) |   C# event / call (ns) |
|-------------------:|:------------|:------------------------|:---------------------------|------------------------:|--------------------------:|-----------------------:|
|                  1 | C# event    | -79.0%                  | +7.6%                      |                   1.804 |                     1.941 |                  0.378 |
|                  4 | MessagePipe | +209.3%                 | -18.5%                     |                   3.679 |                     2.997 |                 11.377 |
|                 16 | LayerBase   | +499.1%                 | +56.2%                     |                   6.341 |                     9.904 |                 37.986 |
|                 64 | LayerBase   | +750.8%                 | +98.3%                     |                  17.631 |                    34.954 |                149.999 |

## 4. Cross-Framework: Scaling Summary

This table shows which implementation scales more gracefully as fanout rises from 1 to 64 subscribers.

| Framework   | 1 subscriber (us)   | 64 subscribers (us)   | Growth 1→64   |
|:------------|:--------------------|:----------------------|:--------------|
| C# event    | 378.3               | 149,999.3             | 396.51x       |
| LayerBase   | 1,804.0             | 17,630.7              | 9.77x         |
| MessagePipe | 1,940.8             | 34,953.8              | 18.01x        |

## 5. Internal LayerBase: Sync vs Notify

![LayerBase Internal Fanout - Sync vs Notify](release_internal_sync_vs_notify_bar.png)

|   Subscriber Count |   Sync (ms) |   Notify (ms) |
|-------------------:|------------:|--------------:|
|                  1 |       3.301 |         1.78  |
|                  4 |       5.678 |         3.964 |
|                 16 |      10.816 |         6.158 |
|                 64 |      34.058 |        17.245 |

## 6. Internal LayerBase: Notify Gain Over Sync

![LayerBase Notify Gain Over Sync](release_internal_notify_speedup_bar.png)

|   Subscriber Count |   Sync (ms) |   Notify (ms) | Notify faster than Sync   |   Sync / call (ns) |   Notify / call (ns) | Notify growth vs 1x   |
|-------------------:|------------:|--------------:|:--------------------------|-------------------:|---------------------:|:----------------------|
|                  1 |       3.301 |         1.78  | 46.1%                     |              3.301 |                1.78  | 1.00x                 |
|                  4 |       5.678 |         3.964 | 30.2%                     |              5.678 |                3.964 | 2.23x                 |
|                 16 |      10.816 |         6.158 | 43.1%                     |             10.816 |                6.158 | 3.46x                 |
|                 64 |      34.058 |        17.245 | 49.4%                     |             34.058 |               17.245 | 9.69x                 |

## 7. Release Takeaways

### Cross-framework
- At **1 subscriber**, LayerBase is now close enough to MessagePipe that the difference is minor in practice.
- At **4 subscribers**, MessagePipe still has an advantage.
- At **16 subscribers** and **64 subscribers**, LayerBase becomes the fastest of the three compared implementations.

### Internal LayerBase
- The Notify path is faster than the Sync path at every measured fanout level.
- The gain is largest when fanout becomes large, which means the Notify path is not only cheaper at the low end, but also scales better.

## 8. Suggested README Positioning

Recommended phrasing for the repository README:

> **LayerBase's Notify path is now competitive with MessagePipe at single-subscriber dispatch, and overtakes it as fan-out grows.**
>
> **Inside LayerBase itself, Notify remains consistently faster than Sync across all tested fanout levels.**

## 9. Included Files

### Raw tables
- `release_single_subscriber_raw.csv`
- `release_notify_fanout_raw.csv`
- `release_internal_sync_vs_notify_raw.csv`

### Analysis tables
- `release_notify_fanout_analysis.csv`
- `release_notify_scaling_summary.csv`
- `release_internal_sync_vs_notify_analysis.csv`

### Charts
- `release_single_subscriber_bar.png`
- `release_notify_fanout_bar_linear.png`
- `release_notify_fanout_bar_log.png`
- `release_internal_sync_vs_notify_bar.png`
- `release_internal_notify_speedup_bar.png`
