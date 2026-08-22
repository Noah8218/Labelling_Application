#!/usr/bin/env python3
"""Create one reviewable MobileSAM mask from an operator box and correction points."""

from __future__ import annotations

import argparse
import hashlib
import json
import sys
import time
from pathlib import Path
from typing import Any


def file_sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for block in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(block)
    return digest.hexdigest().upper()


def compact_json(value: dict[str, Any]) -> str:
    return json.dumps(value, ensure_ascii=False, separators=(",", ":"))


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser()
    parser.add_argument("--weights", default="")
    parser.add_argument("--image", default="")
    parser.add_argument("--x", type=int, default=0)
    parser.add_argument("--y", type=int, default=0)
    parser.add_argument("--width", type=int, default=0)
    parser.add_argument("--height", type=int, default=0)
    parser.add_argument(
        "--point",
        action="append",
        default=[],
        help="Repeatable x,y,label point prompt where label is 1 (positive) or 0 (negative).",
    )
    parser.add_argument("--max-polygon-points", type=int, default=96)
    parser.add_argument("--device", default="cpu")
    parser.add_argument("--self-test", action="store_true")
    return parser.parse_args()


def self_test() -> int:
    prompt = [10, 20, 40, 60]
    assert prompt[2] > prompt[0] and prompt[3] > prompt[1]
    points = parse_prompt_points(["15,25,1", "35,45,0"])
    assert points == [[15.0, 25.0, 1], [35.0, 45.0, 0]]
    assert len(limit_polygon_points(list(range(200)), 48)) == 48
    print(
        compact_json(
            {
                "success": True,
                "mode": "self-test",
                "promptBox": prompt,
                "promptPoints": points,
            }
        )
    )
    return 0


def parse_prompt_points(values: list[str]) -> list[list[float | int]]:
    points: list[list[float | int]] = []
    for value in values:
        parts = value.split(",")
        if len(parts) != 3:
            raise ValueError(f"point prompt must be x,y,label: {value}")
        x, y = float(parts[0]), float(parts[1])
        label = int(parts[2])
        if x < 0 or y < 0 or label not in (0, 1):
            raise ValueError(f"invalid point prompt: {value}")
        points.append([x, y, label])
    return points


def limit_polygon_points(points: list[Any], maximum: int) -> list[Any]:
    """Select evenly spaced contour vertices without changing source or model output."""
    maximum = max(16, min(1024, int(maximum)))
    if len(points) <= maximum:
        return points
    return [points[(index * len(points)) // maximum] for index in range(maximum)]


def run(args: argparse.Namespace) -> int:
    weights = Path(args.weights).expanduser().resolve()
    image = Path(args.image).expanduser().resolve()
    if not weights.is_file():
        raise FileNotFoundError(f"MobileSAM weights not found: {weights}")
    if not image.is_file():
        raise FileNotFoundError(f"prompt image not found: {image}")
    if args.width <= 0 or args.height <= 0:
        raise ValueError("prompt box width and height must be positive")
    prompt_points = parse_prompt_points(args.point)

    from ultralytics import SAM, __version__ as ultralytics_version
    import torch

    left = max(0, args.x)
    top = max(0, args.y)
    right = left + args.width
    bottom = top + args.height
    started = time.perf_counter()
    model = SAM(str(weights))
    predict_arguments: dict[str, Any] = {
        "source": str(image),
        "bboxes": [left, top, right, bottom],
        "device": args.device,
        "verbose": False,
    }
    if prompt_points:
        # Box batch is one object, so all correction points belong to that same prompt batch.
        predict_arguments["points"] = [[[point[0], point[1]] for point in prompt_points]]
        predict_arguments["labels"] = [[point[2] for point in prompt_points]]
    results = model.predict(**predict_arguments)
    elapsed_ms = (time.perf_counter() - started) * 1000.0
    result = results[0] if results else None
    masks = getattr(result, "masks", None)
    polygons = list(getattr(masks, "xy", []) or [])
    if not polygons:
        raise RuntimeError("MobileSAM returned no mask for the prompt box")

    polygon = limit_polygon_points(
        list(max(polygons, key=lambda points: len(points))),
        args.max_polygon_points,
    )
    points = [
        {"x": round(float(point[0]), 3), "y": round(float(point[1]), 3)}
        for point in polygon
    ]
    if len(points) < 3:
        raise RuntimeError("MobileSAM returned a mask contour with fewer than three points")

    xs = [point["x"] for point in points]
    ys = [point["y"] for point in points]
    mask_area = int(masks.data[0].sum().item()) if getattr(masks, "data", None) is not None else 0
    original_shape = list(getattr(result, "orig_shape", []) or [])
    image_height = int(original_shape[0]) if len(original_shape) > 0 else 0
    image_width = int(original_shape[1]) if len(original_shape) > 1 else 0
    output = {
        "success": True,
        "mode": "box-and-point-prompt" if prompt_points else "box-prompt",
        "model": "MobileSAM",
        "weightsPath": str(weights),
        "weightsSha256": file_sha256(weights),
        "imagePath": str(image),
        "imageWidth": image_width,
        "imageHeight": image_height,
        "promptBox": [left, top, right, bottom],
        "promptPoints": [
            {"x": point[0], "y": point[1], "label": point[2]}
            for point in prompt_points
        ],
        "maximumPolygonPoints": max(16, min(1024, int(args.max_polygon_points))),
        "bounds": {
            "x": min(xs),
            "y": min(ys),
            "width": max(xs) - min(xs),
            "height": max(ys) - min(ys),
        },
        "polygon": points,
        "maskArea": mask_area,
        "elapsedMs": round(elapsed_ms, 3),
        "device": args.device,
        "ultralyticsVersion": ultralytics_version,
        "torchVersion": torch.__version__,
    }
    print(compact_json(output))
    return 0


def main() -> int:
    args = parse_args()
    if args.self_test:
        return self_test()
    try:
        return run(args)
    except Exception as error:
        print(
            compact_json(
                {
                    "success": False,
                    "errorCode": type(error).__name__,
                    "error": str(error),
                }
            ),
            file=sys.stderr,
        )
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
