# Recipe Dataset Version v2

Date: 2026-07-23

## Purpose

Recipe Dataset Version v2 answers a reproducibility question that the former
count-based ID could not answer:

> Which exact images, annotations, class order, and split ownership were used by
> this training or model-comparison result?

The former ID could remain unchanged when a box or polygon coordinate changed
but the number of objects stayed the same. Version v2 hashes the actual content
and therefore treats that edit as a new dataset version.

## Contract

Included:

- deterministic SHA-256 over recipe-owned image and annotation file contents;
- relative path, artifact kind, and train/valid/test ownership in the identity;
- ordered class-name contract and exact split ownership;
- image-level anomaly review data;
- metadata-only immutable history under `dataset.versions`;
- exact Dataset Version and content SHA-256 on training-run/model history;
- exact exported train/valid/test ownership for anomaly-classification images;
- an existing external native YOLO source fingerprint mapped to an explicit
  external Dataset Version without modifying the source.

Excluded:

- copying or snapshotting the full dataset;
- cloud sync, accounts, reviewer assignment, comments, or branch merging;
- automatic model adoption;
- a claim that identical data implies identical model quality.

Identity format:

```text
dsv2-<64 lowercase SHA-256 characters>
```

External native YOLO identity format:

```text
dsv2-external-yolo-<existing source content fingerprint>
```

## Operator Workflow

1. Save the Recipe or start training.
2. Open `4 학습/모델` → `데이터` → `프로젝트`.
3. Confirm `데이터 버전` and the shortened content SHA-256.
4. Train or compare models. The candidate/model history retains the Dataset
   Version used for that run.
5. If a label coordinate, class order, file content, or split ownership changes,
   save again and verify that a new version appears.

Repeated saves of unchanged content reuse the existing version-history entry.
The history stores hashes and counts only; source images and labels remain in
their original locations. Training-progress metadata saves reuse the version
captured at training start and do not rescan the dataset on every status update.

## Completion Evidence

- Same image/label/class/split content produced the same Dataset Version.
- Re-saving unchanged content did not add or overwrite a history entry.
- Changing label geometry without changing object count produced a new version.
- Changing class order/content produced a new version.
- Moving the same file content from `train` to `valid` produced a new version.
- Moving an anomaly-classification export between train and test produced a new
  version.
- Recipe save left the source tree SHA-256 unchanged.
- Training requests, training history, registry entries, and Model Center
  history retained the exact Dataset Version and content SHA-256.
- External native YOLO intake retained its original source fingerprint and
  received an external Dataset Version identity.
- Current-source and actual-EXE 1920x1080 captures showed the same v2 identity,
  SHA-256 summary, image/label counts, and immutable-history count.

Verification:

```powershell
dotnet build .\tests\LabelingApplication.Tests\LabelingApplication.Tests.csproj -c Debug --no-restore /nr:false -m:1 /nodeReuse:false /p:BuildInParallel=false /p:UseSharedCompilation=false /p:OutDir=artifacts\isolated-out\
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --recipe-dataset-version-v2
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --wpf-project-config-panel
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --model-registry
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --wpf-dataset-setup-request
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --wpf-anomaly-purpose-flow
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --external-yolo-dataset-intake
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --dataset-readiness-purpose
dotnet .\tests\LabelingApplication.Tests\artifacts\isolated-out\LabelingApplication.Tests.dll --exe-dataset-version-smoke --exe .\artifacts\isolated-out\OpenVisionLab.LabelingStudio.exe
```

UI evidence:

- `artifacts/ui/recipe-dataset-version-v2/before-dataset-version-1920.png`
- `artifacts/ui/recipe-dataset-version-v2/after-dataset-version-1920.png`
- `artifacts/ui/recipe-dataset-version-v2/exe-dataset-version-1920.png`

The first EXE smoke attempt used four arbitrary bytes with a `.png` extension.
Recipe loading correctly failed while decoding that invalid fixture and reported
`Out of memory`. The harness now creates a real PNG before applying the Recipe;
the current EXE then passed. This was a test-fixture defect, not accepted product
evidence.

## Durable Closure

Status: Complete

Scope: deterministic local Recipe Dataset Version v2, immutable metadata history,
training/model provenance, and read-only Model Center presentation.

Acceptance criteria: same content -> same identity (pass); label/class/split
change -> new identity (pass); source immutability (pass); training/model link
(pass); current-source and actual-EXE UI evidence (pass).

Verification: zero-warning/zero-error isolated build; seven focused contract and
regression switches passed; actual-EXE dataset-version smoke passed.

Evidence: this document, `dataset.versions` metadata contract, focused test
output, and the three UI artifacts listed above.

Boundary / next dependency: no dataset content duplication, collaboration,
cloud/version-control system, field-quality claim, or automatic model adoption.
Independent camera/session data is still required for field-generalization
evidence.
