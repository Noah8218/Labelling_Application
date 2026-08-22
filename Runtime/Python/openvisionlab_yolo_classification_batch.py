from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import sys
from pathlib import Path
from types import ModuleType
from typing import Any, Iterable


SUPPORTED_IMAGE_SUFFIXES = {".bmp", ".jpg", ".jpeg", ".png", ".tif", ".tiff"}


def load_worker_module(worker_script: Path) -> ModuleType:
    spec = importlib.util.spec_from_file_location("openvisionlab_runtime_adapter", worker_script)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Unable to load YOLO adapter: {worker_script}")
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


def class_images(dataset_root: Path, split: str, class_name: str) -> Iterable[Path]:
    class_root = dataset_root / split / class_name
    if not class_root.is_dir():
        return []
    return sorted(
        path.resolve()
        for path in class_root.iterdir()
        if path.is_file() and path.suffix.lower() in SUPPORTED_IMAGE_SUFFIXES
    )


def first_decision_candidate(candidates: list[dict[str, Any]]) -> dict[str, Any] | None:
    for candidate in candidates:
        if candidate.get("imageLevel") is True and candidate.get("candidateType") == "imageClassification":
            return candidate
    for candidate in candidates:
        if candidate.get("imageLevel") is True:
            return candidate
    return candidates[0] if candidates else None


def heatmap_path(evidence_output: Path, expected_class_name: str, image_path: Path) -> Path:
    identity = hashlib.sha256(str(image_path).encode("utf-8")).hexdigest()[:12]
    return evidence_output / expected_class_name / f"{image_path.stem}-{identity}-patchcore.png"


def run(args: argparse.Namespace) -> int:
    worker_module = load_worker_module(Path(args.worker_script).resolve())
    detector_args = argparse.Namespace(
        weights=str(Path(args.weights).resolve()),
        model_root=str(Path(args.model_root).resolve()),
        image_root=str(Path(args.dataset_root).resolve()),
        device=args.device,
        img_size=args.img_size,
        conf=args.conf,
        iou=args.iou,
        max_candidates=args.max_candidates,
        model=args.model,
        debug=args.debug,
    )
    detector = worker_module.build_detector(detector_args)
    detector.load()

    dataset_root = Path(args.dataset_root).resolve()
    for expected_class_name in ("normal", "abnormal"):
        for image_path in class_images(dataset_root, args.split, expected_class_name):
            if args.model.lower() == "patchcore":
                output_path = heatmap_path(Path(args.evidence_output).resolve(), expected_class_name, image_path)
                output_path.parent.mkdir(parents=True, exist_ok=True)
                candidates, image = detector.detect_path(image_path, heatmap_output=output_path)
            else:
                candidates, image = detector.detect_path(image_path)
            candidate = first_decision_candidate(candidates)
            if candidate is None:
                raise RuntimeError(f"Anomaly adapter returned no decision candidate for {image_path}")
            localizations = [
                {
                    "x": float(item.get("x") or 0.0),
                    "y": float(item.get("y") or 0.0),
                    "width": float(item.get("width") or 0.0),
                    "height": float(item.get("height") or 0.0),
                    "anomalyScore": item.get("anomalyScore"),
                    "anomalyThreshold": item.get("anomalyThreshold"),
                    "heatmapPath": str(item.get("heatmapPath") or ""),
                }
                for item in candidates
                if item.get("candidateType") == "anomalyLocalization"
            ]
            print(
                json.dumps(
                    {
                        "imagePath": str(image_path),
                        "expectedClassName": expected_class_name,
                        "predictedClassName": str(candidate.get("className") or ""),
                        "confidence": float(candidate.get("confidence") or 0.0),
                        "predictionType": str(candidate.get("predictionType") or args.model),
                        "anomalyScore": candidate.get("anomalyScore", image.get("anomalyScore")),
                        "anomalyThreshold": candidate.get("anomalyThreshold", image.get("anomalyThreshold")),
                        "heatmapPath": str(candidate.get("heatmapPath") or image.get("heatmapPath") or ""),
                        "localizationCount": len(localizations),
                        "localizations": localizations,
                    },
                    ensure_ascii=True,
                    separators=(",", ":"),
                ),
                flush=True,
            )
    return 0


def self_test() -> int:
    preferred = first_decision_candidate(
        [
            {"className": "ignored"},
            {
                "candidateType": "imageClassification",
                "imageLevel": True,
                "className": "normal",
                "confidence": 0.91,
            },
        ]
    )
    assert preferred is not None
    assert preferred["className"] == "normal"
    assert first_decision_candidate([
        {
            "candidateType": "anomalyLocalization",
            "imageLevel": True,
            "className": "abnormal",
        }
    ])["className"] == "abnormal"
    assert first_decision_candidate([]) is None
    print("self-test passed", flush=True)
    return 0


def parse_args(argv: list[str]) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Evaluate image-level anomaly decisions with one persistent adapter model load.")
    parser.add_argument("--worker-script", default="")
    parser.add_argument("--weights", default="")
    parser.add_argument("--model-root", default="")
    parser.add_argument("--dataset-root", default="")
    parser.add_argument("--split", default="test")
    parser.add_argument("--device", default="cpu")
    parser.add_argument("--img-size", type=int, default=64)
    parser.add_argument("--conf", type=float, default=0.0)
    parser.add_argument("--iou", type=float, default=0.45)
    parser.add_argument("--max-candidates", type=int, default=20)
    parser.add_argument("--model", default="yolov8")
    parser.add_argument("--evidence-output", default=".")
    parser.add_argument("--debug", action="store_true")
    parser.add_argument("--self-test", action="store_true")
    return parser.parse_args(argv)


def main(argv: list[str]) -> int:
    args = parse_args(argv)
    if args.self_test:
        return self_test()
    return run(args)


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
