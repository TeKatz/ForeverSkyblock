# PR #28 / #29 test checklist — archived

The implementation represented by this checklist is integrated into `main`.

Final automated validation before integration used source `38a730e77c066ada5ade7395568baa04c1c635ff` and GitHub Actions run **#563**. The following automated stages passed:

- Java build and unit tests;
- headless client smoke test;
- visual fixtures;
- dedicated-server regression suite;
- full pack-runtime integration on NeoForge **21.1.259**.

The final native-coordinate machine-GUI change was merged by explicit project authorization after repeated in-game reports exposed the old scaling approach. This archived checklist does **not** claim a separate post-merge real-player resize acceptance run.

Current implementation details: [RESOURCE_REFINING.md](RESOURCE_REFINING.md). Current repository state: [CURRENT_STATE.md](CURRENT_STATE.md).
