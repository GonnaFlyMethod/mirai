import argparse
import json
from pathlib import Path

import torch

from train_resnet50 import LABELS, build_model, export_onnx


PROJECT_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_WEIGHTS = PROJECT_ROOT / "api" / "api" /  "Models" / "brain-tumor-resnet50.pth"
DEFAULT_ONNX = PROJECT_ROOT / "api" / "api" / "Models" / "brain-tumor-resnet50.onnx"


def main():
    parser = argparse.ArgumentParser(description="Export a trained brain MRI ResNet50 checkpoint to ONNX.")
    parser.add_argument("--weights", type=Path, default=DEFAULT_WEIGHTS)
    parser.add_argument("--onnx", type=Path, default=DEFAULT_ONNX)
    args = parser.parse_args()

    if not args.weights.exists():
        raise FileNotFoundError(f"Checkpoint was not found: {args.weights}")

    args.onnx.parent.mkdir(parents=True, exist_ok=True)
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    model = build_model(device)
    model.load_state_dict(torch.load(args.weights, map_location=device))
    export_onnx(model, args.onnx, device)

    metadata = {
        "labels": LABELS,
        "imageSize": 224,
        "normalization": {"mean": [0.5, 0.5, 0.5], "std": [0.5, 0.5, 0.5]},
        "onnxPath": str(args.onnx),
        "weightsPath": str(args.weights),
    }
    metadata_path = args.onnx.with_name("brain-tumor-resnet50.metadata.json")
    metadata_path.write_text(json.dumps(metadata, indent=2))
    print(json.dumps(metadata, indent=2))


if __name__ == "__main__":
    main()
