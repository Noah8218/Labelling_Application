# Actual EXE Operator Video and GitHub GIF Plan

Date: 2026-07-28

Status: Complete

## Goal

Operate the latest built OpenVisionLab Labeling Studio EXE through real
mouse/keyboard-equivalent input, record the complete interaction, review the
recording as product evidence, fix any material usability defects, and only
then derive a compact GitHub promotional GIF.

The raw evaluation video and the promotional GIF are different artifacts:

- the raw video preserves waits, mistakes, status changes, and full application
  context so product problems remain visible;
- the GIF is a short, truthful excerpt from a passing rerun. It must not hide a
  failure, fabricate a state, or show a result that was seeded internally.

## Existing foundation

- `--exe-smart-mask-point-smoke` already launches the real Debug EXE and drives
  box prompt, positive point, negative point, candidate rerun, Candidate Review
  confirmation, and next-instance transition through UI Automation plus native
  mouse input.
- `--exe-labeling-productivity-smoke` already verifies real shortcut and
  annotation behavior.
- workflow helpers already capture actual EXE screenshots.
- FFmpeg is installed locally and can record the application window and create
  a palette-optimized GIF.

The recording implementation should extend these proven paths rather than
invent another parallel automation framework.

## Included scope

1. Build and verify the latest Debug EXE.
2. Prepare an isolated, sanitized sample Recipe and images.
3. Launch the actual EXE at a deterministic 1920x1080 layout.
4. Operate the application only through visible UI commands, mouse movement,
   mouse drag, keyboard shortcuts, and dialog interaction.
5. Record an uncut operator-evaluation MP4 with a visible cursor.
6. Record timestamped action and status events.
7. Extract key frames and a contact sheet from the MP4.
8. Review the video against the rubric in this document.
9. If a material defect is found, preserve the first video as before evidence,
   fix the defect, rebuild, and rerun.
10. Produce one GitHub hero GIF only from a passing rerun.
11. Add the GIF and a static poster to README only after visual approval.

## Excluded scope

- training a new model;
- presenting synthetic results as production accuracy;
- recording cloud, team, account, video-labeling, camera, PLC, or deployment
  features;
- directly calling ViewModel or service methods to create a post-action UI
  state after the EXE starts;
- recording private Recipe names, local user paths, credentials, or unrelated
  desktop content;
- committing the large raw MP4 to Git;
- adding the GIF to README before the user reviews the final artifact.

## Recording scenarios

### Scenario A: uncut operator evaluation

Purpose: find real workflow and presentation defects.

Target duration: 60-120 seconds.

Steps:

1. Start the latest Debug EXE with a clean temporary workspace configuration.
2. Load the prepared segmentation sample through the visible application flow.
3. Select the next incomplete image from Worklist.
4. Select the box prompt tool.
5. Draw the Smart Mask start box with a visible human-speed drag.
6. Generate the initial candidate and keep the real waiting state visible.
7. Add one positive and one negative point.
8. Rerun the candidate.
9. Inspect the Candidate Review presentation.
10. Confirm the candidate.
11. Explicitly save the annotation.
12. Move to the next incomplete image.
13. Open Dataset Health visual QA and return to the existing editor.
14. Open AI batch preflight and cancel without starting a batch.

This run is intentionally broader than the GIF. It evaluates navigation,
feedback, modal behavior, status clarity, no-autosave safety, and return paths.

### Scenario B: GitHub hero

Purpose: communicate the main product value in one readable loop.

Target source duration: 14-20 seconds.

Storyboard:

| Time | Visible action | Viewer takeaway |
| --- | --- | --- |
| 0-2s | Worklist image and active Recipe are already visible | This is a real labeling workstation |
| 2-5s | Draw Smart Mask box | Operator provides inspection intent |
| 5-8s | Initial candidate appears | AI accelerates the first draft |
| 8-11s | Add positive/negative correction points and rerun | Candidate is correctable, not blindly accepted |
| 11-14s | Candidate Review and explicit confirm | Human review owns the result |
| 14-17s | Explicit save, then next incomplete image | The workflow continues without auto-save ambiguity |
| 17-20s | Hold final state | Viewer can read the completed result |

If the real worker wait makes the source longer, the promotional edit may
remove dead time but must retain a visible busy indicator and must not reorder
events.

## Real-operation boundary

Allowed before EXE launch:

- create temporary sample images and Recipe data;
- isolate workspace/layout settings;
- snapshot files that the smoke must restore;
- position the app window and start recording.

Allowed after EXE launch:

- UI Automation invoke/select/value patterns;
- native cursor movement, click, drag, wheel, and keyboard input;
- read-only UI Automation status inspection;
- screen/window capture.

Not allowed after EXE launch:

- calling application ViewModels or services to seed candidates or labels;
- writing label files to make the UI appear complete;
- replacing the canvas image or candidate state from the test process;
- suppressing a product error and continuing the promotional recording.

## Capture architecture

```text
Current source
  -> Debug build
  -> exact EXE/timestamp check
  -> temporary sanitized fixture
  -> FFmpeg window/desktop crop starts
  -> UI Automation + native input operates actual EXE
  -> action/status JSONL + checkpoint PNGs
  -> FFmpeg stops cleanly
  -> ffprobe metadata + SHA-256
  -> 1 fps contact sheet and key-frame extraction
  -> visual/product review
  -> fix and rerun if needed
  -> approved MP4 excerpt
  -> optimized GitHub GIF + poster PNG
```

The capture runner should start FFmpeg as a hidden child process and terminate
it through standard input (`q`) so the MP4 index is finalized. It should never
kill FFmpeg as the normal completion path.

## Artifact layout

```text
artifacts/operator-video/20260728-<run-id>/
  source/
    actual-exe-operator-run.mp4
    ffprobe.json
    sha256.txt
  evidence/
    events.jsonl
    screenshots/
    keyframes/
    contact-sheet.png
  review/
    self-evaluation.md
    defects.md
  publish/
    github-hero-source.mp4
    github-hero.gif
    github-hero-poster.png
```

Only the final approved GIF and poster are candidates for
`docs/tutorial/images/`. Raw video, logs, local paths, and review notes remain
under ignored artifacts.

## Capture settings

Raw evaluation MP4:

- application window: deterministic 1920x1080;
- capture: 30 fps;
- codec: H.264 `libx264`;
- pixel format: `yuv420p`;
- quality target: CRF 18-20;
- visible cursor;
- no audio;
- no unrelated desktop area.

GitHub hero GIF project budget:

- 1280x720 maximum frame;
- 12-15 fps;
- 14-20 seconds;
- target below 8 MiB;
- hard project budget 10 MiB;
- palette generated from the selected source clip;
- short final-frame hold for readability;
- no captured local filesystem path or personal data.

Example conversion shape:

```powershell
ffmpeg -i github-hero-source.mp4 `
  -vf "fps=15,scale=1280:-2:flags=lanczos,split[s0][s1];[s0]palettegen=stats_mode=diff[p];[s1][p]paletteuse=dither=bayer:bayer_scale=3:diff_mode=rectangle" `
  -loop 0 github-hero.gif
```

The actual command will use explicit start/end timestamps from the approved
event log.

## Self-evaluation rubric

Score each category from 1 to 5 and cite video timestamps.

| Category | Questions |
| --- | --- |
| First impression | Is the main task understandable within five seconds? |
| Visual hierarchy | Are image, current tool/class, next action, and result clearly prioritized? |
| Discoverability | Can an unfamiliar user find the next action without reading every control? |
| Workflow efficiency | Are there unnecessary panel changes, clicks, or cursor travel? |
| Feedback and latency | Does every wait show a truthful busy/progress state? |
| AI review safety | Are pending candidate, confirm, save, and next-image states distinct? |
| Error prevention | Are invalid actions disabled or explained before mutation? |
| Layout quality | Is text readable with no clipping, overlap, or excessive empty space? |
| Consistency | Do icons, labels, status colors, and button placement behave consistently? |
| Promotional clarity | Does the clip show a coherent value proposition without explanation? |

Severity:

- `P0`: data loss, wrong saved result, source mutation, or misleading success;
- `P1`: workflow cannot complete or promotional claim would be false;
- `P2`: material confusion, hidden action, clipping, long unexplained wait, or
  avoidable detour;
- `P3`: polish issue that does not obstruct the workflow.

Promotion gate:

- no P0/P1 issue;
- no unresolved P2 issue visible in the hero sequence;
- every rubric category at least 4/5 for the final hero;
- action order and event timestamps agree with the video;
- final save is explicit and verified;
- source image and fixture baseline hashes are restored or unchanged;
- the GIF is visually reviewed at both native size and README width.

## Review method

1. Watch the full MP4 once without pausing and record first-impression notes.
2. Watch again with the event log and record friction timestamps.
3. Inspect a 1 fps contact sheet for layout changes, popups, stale states,
   clipping, accidental desktop exposure, and visual discontinuities.
4. Inspect key frames before prompt, candidate, corrected candidate, confirm,
   save, Dataset Health, and batch preflight.
5. Compare recorded status text with expected operation completion.
6. Classify each issue P0-P3.
7. Preserve the first recording as before evidence.
8. Make only evidence-backed fixes, rebuild, and rerun the same scenario.
9. Produce the GIF only after the rerun passes.

## README publication design

Proposed public files after approval:

```text
docs/tutorial/images/github/
  labeling-smart-mask-zero-drift-20260805.gif
  labeling-smart-mask-stable-20260805-poster.png
```

Proposed README placement:

- directly after the one-paragraph product summary;
- descriptive alt text explaining box prompt, point correction, review, and
  explicit save;
- one concise caption stating that the GIF uses an isolated sample and shows
  the actual Windows EXE;
- retain static workflow screenshots below for accessibility and readers who
  disable animation.

The README must not contain local paths, test terminology, conversation notes,
private model locations, or claims of production accuracy.

## Implementation sequence

1. Add a dedicated `--exe-operator-video-smoke` runner by reusing the actual
   Smart Mask and EXE UI helpers.
2. Add a small FFmpeg recording adapter in the test harness, not the product.
3. Add event logging and deterministic window positioning.
4. Build the latest EXE and run Scenario A uncut.
5. Extract frames and perform the documented self-evaluation.
6. Fix only observed product defects and rerun Scenario A.
7. Run Scenario B and generate the GIF/poster.
8. Present MP4, contact sheet, review, GIF, and poster to the user.
9. After user approval, copy only the GIF/poster into public docs and update
   README.

## Acceptance criteria for the next implementation

- actual current-build EXE is visibly operated through UI input;
- raw MP4, event log, screenshots, ffprobe metadata, and SHA-256 exist;
- the video is reviewed rather than merely recorded;
- every finding cites a timestamp and severity;
- first-run defects are not edited out of the evaluation evidence;
- final GIF is derived from a passing rerun and meets the project size/frame
  budget;
- source/sample files are unchanged or restored;
- README remained unchanged before explicit visual approval; the later
  superseding execution record documents the approved publication.

## Completion record

Status: Complete

Scope: Design the actual-EXE operation, recording, self-evaluation, repair
loop, GIF conversion, and GitHub publication workflow.

Acceptance criteria:

- existing actual-EXE interaction and capture assets identified: pass;
- raw evaluation evidence separated from promotional editing: pass;
- real-operation and anti-fabrication boundary defined: pass;
- repeatable artifact layout, review rubric, severity, and promotion gate
  defined: pass;
- GitHub GIF and README publication boundary defined: pass.

Verification: repository capture-tool search, actual Smart Mask runner review,
local FFmpeg availability check, documentation policy gate, and
`git diff --check`.

Evidence: this document and existing `--exe-smart-mask-point-smoke` /
`--exe-labeling-productivity-smoke` runners.

Boundary / next dependency: implement the recording runner and execute the
first uncut actual-EXE operator video.

Recommended model: `gpt-5.6-sol`

Reasoning effort: `high`

## 2026-08-05 hover, pacing, and visual-continuity correction

Status: Complete

The first current-UI refresh was reopened twice after direct GIF review. The
first review found a bright framework hover state and slow pacing. The second
review proved that the 5.9-second crop still switched between separately
rasterized viewer layouts: its transition frame moved by up to three pixels,
the candidate state remained two pixels lower, and the image boundary changed
by one pixel. The earlier claim that this edit kept the viewer fixed is
withdrawn and superseded by the evidence below.

### Correction

- `DetectionOverlayActionButtonStyle` now owns an explicit dark control
  template for normal, hover, pressed, keyboard-focus, and disabled states;
- commands, candidate state, confirmation, saving, image selection, and viewer
  auto-fit behavior are unchanged;
- the rejected edit's crossfade between differently scaled WPF viewer frames
  was removed instead of being tuned again;
- the public GIF now renders the actual run's source image once and changes
  only the rough-box, exact saved 96-point candidate, cursor, and saved-label
  overlays on that immutable background;
- the final overlay fades to the byte-identical first background, so repeated
  playback has no viewer reset;
- the README explicitly calls this a fixed-canvas explanatory rendering based
  on the actual EXE source/result. The unchanged poster remains the full actual
  UI evidence;
- the README and tutorial use a fresh media filename to avoid stale GitHub
  image caching.

### Current evidence

- latest actual-EXE run: `20260805-smart-contour-hover-fix-r2`;
- active monitor: `\\.\DISPLAY2`, bounds `-1920,365,1920,1080`;
- raw recording: H.264 1920x1080, 40.267 seconds, 1,204 frames, SHA-256
  `6eaa10ddfe3ff21019386597c25713bd08e0a40401f9705f4863bccb45016c67`;
- saved result: one polygon, 96 points, 6,614 pixels, IoU `0.5801`, precision
  `0.6155`, recall `0.9097`;
- rejected 5.9-second GIF baseline: transition frame 27 best-aligned at
  `(-3px,+3px)`, candidate frame 29 at `(0px,+2px)`, with three distinct
  image-boundary states;
- corrected public GIF: 1024x576, 5.400 seconds, 54 logical 100ms steps stored
  as 24 duration-coalesced GIF frames, 1,731,404 bytes, SHA-256
  `86ba4ea56d26195f0feac7f0eec555850cac2b1d382c32b4eb1675549bba2ae1`;
- corrected frame registration: one image-boundary state `(362,661)`, every
  clean-background frame best-aligned at `(0px,0px)`, maximum clean ROI error
  `0.0`, no changed pixel outside the bounded overlay envelope, and a
  byte-identical first/last decoded frame;
- poster: 1280x720, SHA-256
  `4da08fe8fa40fe73eb7ae8943e7da8e1e8da544fac5bb67ff48583268d26dbc2`.

### Completion record

Status: Complete

Scope: canvas candidate-action hover correction, latest actual-EXE rerecording,
rejection of the still-moving crop, zero-drift fixed-canvas README GIF,
truthful public wording, and visual before/after review.

Acceptance criteria:

- candidate confirmation remains readable during pointer hover: pass;
- actual automatic-contour confirm/save workflow remains functional: pass;
- public GIF is materially faster than the superseded 20-second and rejected
  5.9/8-second edits: pass (`5.400` seconds);
- layout-width changes cannot move or resample the displayed background:
  pass, one immutable background is used for every frame;
- background registration and loop boundary: pass, `(0px,0px)`, clean ROI
  error `0.0`, and first/last decoded-frame difference `0`;
- intermediate full-black frames and a hard last-to-first reset are absent:
  pass, checked over more than two loops in the tutorial at a rendered width of
  894px;
- fresh public media paths are linked without stale references: pass.

Verification: isolated test build with zero warnings/errors;
`--wpf-canvas-detection-overlay`; current Debug application build with zero
warnings/errors; 1366x768 candidate-workbench responsive layout;
`--exe-operator-video-smoke --verify-auto-contour-mode` on the leftmost
monitor; raw/GIF ffprobe; SHA-256; rejected/corrected pixel-registration
comparison; transition and loop-boundary contact sheets; repeated in-browser
playback at 894px; final GIF and poster visual review.

Evidence: `0. UI\9) WPF\Views\WpfCanvasPanel.xaml`,
`tests\LabelingApplication.Tests\Program.cs`, the current README media, and the
ignored D-drive run/review folders `20260805-smart-contour-hover-fix-r2` and
`readme-gif-zero-drift-20260805`.

Boundary / next dependency: this proves the local current-build Smart Mask
demo path and its dark hover presentation. It does not establish field
segmentation accuracy or complete the separate GPU-capable clean-target P0-C
gate.

## 2026-08-05 operational viewer stability and real-EXE GIF correction

Status: Complete

The earlier fixed-canvas explanatory rendering hid the product defect instead
of fixing it. This correction supersedes that presentation-only workaround.

### Product correction

- inference-result application no longer calls `ZoomToFit` immediately and
  again at Render/ApplicationIdle; `MainCanvasView.SizeChanged` remains the
  single owner of one coalesced fit after a real viewport-size change;
- the detection summary shares the existing fixed-height layer strip instead
  of adding an Auto row above the viewer;
- Smart Mask guidance is hosted by a non-sizing overlay Canvas;
- annotation-toolbar items use one 29px line height, so a wrapped row cannot
  gain five pixels when actions become visible;
- visible candidate confirmation remains in Candidate Review, with a stable
  automation identifier for the actual-EXE workflow.

### Current evidence

- latest run: `20260805-viewer-stability-after-r7` on leftmost
  `\\.\DISPLAY2`, bounds `-1920,365,1920,1080`;
- actual EXE flow: rough box -> automatic MobileSAM candidate -> explicit
  confirmation -> save, one 96-point polygon and 6,629 mask pixels;
- six raw-video checkpoints from request-before through save-after all use
  image bounds `877,383,232,589`;
- all 65 decoded public-GIF frames use scaled image bounds
  `468,204,123,314`;
- public GIF: real EXE pixels, 1024x576, 10fps, 6.500 seconds, 65 frames,
  523,339 bytes, SHA-256
  `f3f5e69b9f9f9cf5624e33a95b5a9a6332c2203aa0cf790f190130eaf57c39b4`;
- poster: real EXE candidate-review frame, 1280x720, 360,844 bytes, SHA-256
  `de6437f0f4e21cd9b59217232182a29cfaae577b6fc6aeb834a62659c4c170fa`.

### Completion record

Status: Complete

Scope: operational WPF viewer stability during inference/Smart Mask state
changes, actual-EXE before/after review, and replacement of the synthetic
README media with a faster actual-EXE excerpt.

Acceptance criteria:

- no forced post-inference Fit sequence: pass;
- result/guidance visibility does not resize the viewer: pass;
- image position and scale remain identical across request, generation,
  review, confirm, and save: pass;
- candidate confirmation and explicit save remain functional: pass;
- public GIF is derived directly from the passing EXE recording: pass;
- existing protected behavior remains intact: pass, default `267/267` suite.

Verification: zero-warning/error isolated and current-solution builds;
`--wpf-canvas-detection-overlay`; `--wpf-detection-display-mode`;
`--wpf-batch-detection-result`; actual-EXE
`--exe-operator-video-smoke --verify-auto-contour-mode`; raw/GIF frame-bound
measurement; GIF/poster/contact-sheet visual review; `--priority-workflow-docs`;
documentation IA; single-runner default protected suite.

Evidence: the WPF sources and tests named above, current README media, and the
D-drive run folders `20260805-viewer-stability-before-r1` and
`20260805-viewer-stability-after-r7`.

Boundary / next dependency: this proves current-workstation operational
stability and demo truth, not field segmentation accuracy. The separate P0-C
GPU-capable clean-Windows target remains externally blocked.

## 2026-08-05 current-UI README GIF refresh

Status: Complete

The README hero was rerecorded after the labeling workbench and automatic-
contour workflow changed. Product code was not changed for the capture. The
actual current Debug EXE was operated on the active leftmost monitor, and the
test-only operator runner was updated to accept a left monitor with negative
virtual-desktop coordinates.

### Current operation proven

- source EXE SHA-256:
  `c40939e0dcbb28c12ab40e19b500911b03fece67feb5bc55957a28a420c728b1`;
- active monitor: `\\.\DISPLAY2`, bounds `-1920,365,1920,1080`;
- actual public sample: official KolektorSDD fine annotation
  `kos14/Part7.jpg` and `Part7_label.bmp`;
- visible sequence: enable `자동 윤곽` once, draw one rough box, automatically
  fit the viewer after the workflow panel changes, generate a MobileSAM
  candidate without another action click, explicitly confirm/save, then open
  the next incomplete image;
- no Fit action and no separate Smart Mask create action appear in the event
  sequence;
- saved result: one polygon, 96 points, and 6,614 mask pixels;
- fine-mask comparison: IoU `0.5801`, precision `0.6155`, recall `0.9097`.

The previous one-sided `precision >= 0.90` runner gate was tied to an older
19,808-pixel broad mask. The restored official fine mask contains 4,475
positive pixels. The runner now keeps an objective human-review gate while
requiring IoU, precision, and recall together; it does not lower the gate to a
single unchecked visual judgment.

### Recording and publication evidence

- passing run ID: `20260805-smart-contour-readme-refresh-r5`;
- uncut application-window recording: 1920x1080 H.264, 57.833 seconds, 1,728
  frames, SHA-256
  `9c0601464ff41549af97ca03656dde106039569d180e318bfd08c7ba34a86a76`;
- public GIF: 1024x576, 10fps, 20.000 seconds, 200 frames, 1,269,047 bytes,
  SHA-256
  `0781474c1ac07d7c9ec06f355c31073112467236760ad472f3008362c4346155`;
- poster: 1280x720 PNG, 347,442 bytes, SHA-256
  `c56110cc9fca70f10aaa4a7ddce8d9b246e1326d0c1b9c162f14ba0089cead03`;
- ignored local evidence:
  `artifacts\operator-video\20260805-smart-contour-readme-refresh-r5`.

The promotional edit removes only stationary cursor travel and the middle of
the inference wait, retains the visible busy state and original event order,
and plays at about 1.35x. The bottom execution-log strip is cropped because the
uncut evidence shows a machine-local test path there. The uncut MP4 remains
available for audit, and no failed action, candidate state, or save result is
hidden.

### Visual and operational review

- no unrelated desktop or application is visible;
- dark-theme controls, focus states, candidate review, saved state, and image
  queue remain visually consistent without clipping;
- viewer centering survives the left workflow-panel expansion without a Fit
  click;
- automatic inference remains pending until explicit operator confirmation;
- the full random test Recipe identifier remains a minor P3 presentation
  distraction, not an operational blocker;
- the fine-mask score is one public-sample candidate measurement, not field
  accuracy or commercial-parity evidence.

### Completion record

Status: Complete

Scope: current-UI actual-EXE Smart Mask operation review, leftmost-monitor
recording, saved-artifact validation, public GIF/poster replacement, and README
workflow wording refresh.

Acceptance criteria:

- current EXE automatic-contour labeling/save/next-image path: pass;
- no extra candidate-generation or Fit click: pass;
- application-only leftmost-monitor evidence: pass;
- objective saved mask/segment validation: pass;
- current public GIF/poster tracked and linked from README: pass;
- public-media path exposure removed while uncut evidence remains: pass.

Verification:

- isolated test build: warning 0, error 0;
- `--mobile-sam-box-prompt`: pass;
- `--smart-mask-auto-boundary-presentation`: pass;
- `--wpf-labeling-shell`: pass;
- real MobileSAM box prompt: pass, 96 points;
- `--exe-operator-video-smoke --verify-auto-contour-mode`: pass;
- ffprobe, SHA-256, saved artifacts, full contact sheet, GIF contact sheet, and
  poster visual review: pass.

Evidence: `tests\LabelingApplication.Tests\Program.OperatorVideo.cs`, the two
README media files under `docs\tutorial\images\github`, and the ignored run
folder named above.

Boundary / next dependency: this closes the current README hero refresh, not
production segmentation quality or the separate P0-C clean GPU-target labeling
gate. A directly accessible GPU-capable clean Windows target is still required
for that external package validation.

## 2026-07-28 execution and review record

Status: Complete

### Final accepted run

- run ID: `20260728-175240`
- actual sample: KolektorSDD `kos48/Part5.jpg` with class `Defect`
- recording: actual Debug EXE, exact application-window-title capture, visible
  cursor, 1920x1080, 30fps, H.264, 31.47 seconds
- interaction: select Polygon, visible Fit, place four vertices around the
  crack, close the polygon, review the object, explicitly save, and invoke
  Next Incomplete
- cursor: cubic-eased curved movement at approximately 60Hz input sampling;
  424 emitted movement samples are preserved in the real recording
- saved result: one four-point polygon and 26,537 mask pixels
- mask comparison: IoU `0.9555`, precision `0.9904`, recall `0.9644`
- visual review: no desktop, unrelated application, clipping, overlap, or
  discontinuous workflow state in the final contact sheet, key frames, or
  checkpoint screenshots
- promotional candidate: `defect-labeling-hero.gif`, 1024x718, 12fps, 15.91
  seconds, 706,201 bytes; matching poster PNG also generated

### Rejected runs and product findings

The first technically complete Smart Mask run is intentionally preserved and
is not used for promotion. Its point-corrected result over-segmented normal
surface around the crack: IoU `0.3306`, precision `0.4148`, recall `0.6195`.
This is a P1 promotional-truth defect and direct evidence that current Smart
Mask usability/accuracy on thin industrial cracks remains below the desired
commercial standard.

One intermediate run was also rejected because another OpenVisionLab window
covered Labeling Studio during a worker wait and intercepted Confirm. The
recorder was changed from desktop-region capture to exact application-window
title capture, and the application is restored to the foreground before
critical post-wait actions.

### Final rubric summary

The accepted manual-polygon hero scores at least 4/5 in every rubric category.
No P0/P1/P2 issue remains visible in the derived hero sequence. The full-window
source retains one P3 presentation issue: generous black canvas margins and a
dense header reduce focus. The promotional crop reduces that margin without
changing event order or hiding a failed action.

### Completion record

Status: Complete

Scope: actual-EXE defect labeling recorder, human cursor motion, application-
only capture, timestamped evidence, objective saved-mask comparison, visual
self-review, and candidate GIF/poster generation.

Acceptance criteria:

- actual current Debug EXE visibly operated through UI input: pass;
- real Defect sample and label creation/save shown: pass;
- desktop and unrelated content excluded: pass;
- raw MP4, event log, screenshots, key frames, ffprobe, and SHA-256: pass;
- rejected Smart Mask evidence retained and not promoted: pass;
- final saved label IoU at least 0.85: pass (`0.9555`);
- GIF frame/size budget: pass.

Verification:

- isolated `LabelingApplication.Tests` build: warning 0, error 0;
- `--exe-operator-video-smoke`: pass;
- ffprobe, full contact sheet, 1fps key frames, checkpoint screenshots, saved
  mask/segment validation, and mask comparison: pass;
- GIF contact-sheet and poster review: pass.

Evidence: `tests\LabelingApplication.Tests\Program.OperatorVideo.cs` and ignored
local artifacts under `artifacts\operator-video\20260728-175240`.

Boundary / next dependency: the GIF and poster are review candidates only.
Copying them into public docs and editing README requires explicit user visual
approval. The Smart Mask thin-crack accuracy gap remains an independent
product issue; this manual-polygon hero does not claim AI accuracy or
commercial parity.

## 2026-07-28 superseding automatic Smart Mask record

Status: Complete

The manual-polygon run above remains historical evidence but is superseded as
the promotional candidate. It does not communicate the CVAT/V7-style automatic
labeling value requested by the operator.

### Accepted automatic flow

- run ID: `20260728-smartmask-final4`
- actual sample: KolektorSDD `kos14/Part7.jpg`, class `Defect`
- visible sequence: select Box, draw one rough box, request Smart Mask, review
  a blue filled candidate with `자동 경계 96점`, confirm, verify saved
  segmentation artifacts, and open Next Incomplete
- actual EXE recording: 1920x1080, 30fps H.264, visible human-path cursor,
  exact application-window capture, 38.2 seconds
- saved result: one polygon, 96 boundary points, 7,931 mask pixels
- broad source-label comparison: IoU `0.3927`, precision `0.9861`, recall
  `0.3948`
- promotional GIF: 1024x576, 10fps, 18.7 seconds, 1,021,823 bytes
- poster: 1280x720 PNG

The source mask is a broad rectangular cover while MobileSAM follows the
visible Y-shaped crack. Therefore precision supports candidate containment,
but the IoU/recall mismatch is recorded as label-granularity evidence and is
not used for a production-accuracy claim.

### Product change proven by before/after evidence

Before, Smart Mask already returned a contour and 96 polygon points, but the
pending result read primarily as an outline. The accepted current build:

- rasterizes only the selected pending Smart Mask candidate for an accurate
  semi-transparent blue fill;
- suppresses a duplicate mask badge while retaining the candidate contour and
  review badge;
- exposes `자동 경계 N점` in Candidate Review;
- leaves confirmed labels, brush masks, general detections, inference, and
  persistence unchanged.

Optional positive/negative point correction remains available, but it is not
performed automatically or shown in the accepted hero because both tested
correction placements reduced this sample's precision. The automatic initial
candidate is the correct operator choice for this image.

### Completion record

Status: Complete

Scope: CVAT/V7-style rough-box to automatic filled mask and boundary
presentation, actual-EXE operation, canonical save proof, self-review, and
promotional GIF/poster generation.

Acceptance criteria:

- real box prompt produces a visible filled candidate: pass;
- automatic boundary count is visible before confirmation: pass (`96`);
- no direct ViewModel state seeding or label-file fabrication: pass;
- canonical segment/mask artifacts are created only after confirmation: pass;
- source image remains unchanged: pass;
- application-only capture with human cursor and no desktop exposure: pass;
- promotion size/duration budget: pass.

Verification:

- isolated test build: warning 0, error 0;
- current Debug EXE build: warning 0, error 0;
- `--exe-operator-video-smoke`: pass;
- raw MP4, event log, screenshots, 1fps key frames, contact sheet, ffprobe,
  SHA-256, saved artifact validation, and visual review: pass;
- GIF: 1024x576, 10fps, 18.7 seconds, 1,021,823 bytes.

Evidence: `tests\LabelingApplication.Tests\Program.OperatorVideo.cs` and ignored
local artifacts under
`artifacts\operator-video\20260728-smartmask-final4`.

Boundary / next dependency: this proves one real local Smart Mask review flow,
not CVAT/V7 parity or field accuracy. The user approved publication on
2026-07-28; the GIF/poster are now under `docs/tutorial/images/github/` and the
README embeds the actual-EXE GIF with a static-poster link. The next labeling
UX slice is an auto-first contextual correction panel: keep box-to-auto as the
default and reveal point/detail controls only when the operator asks to correct
a poor candidate.

Recommended model: `gpt-5.6-sol`

Reasoning effort: `high`
