import argparse
import json
from pathlib import Path

import torch
import torch.nn as nn
import torch.optim as optim
from torch.utils.data import DataLoader, random_split
from torchvision import datasets, models, transforms


LABELS = ["glioma", "meningioma", "notumor", "pituitary"]
PROJECT_ROOT = Path(__file__).resolve().parents[1]
DEFAULT_DATASET = PROJECT_ROOT / "datasets" / "brain-mri"
DEFAULT_OUTPUT = PROJECT_ROOT / "api" / "api" / "Models"


def validate_dataset_root(dataset_root: Path):
    missing_paths = []

    for split in ["Training", "Testing"]:
        for label in LABELS:
            class_path = dataset_root / split / label
            if not class_path.is_dir():
                missing_paths.append(class_path)

    if missing_paths:
        expected_structure = "\n".join(
            [
                str(dataset_root),
                "  Training/",
                "    glioma/",
                "    meningioma/",
                "    notumor/",
                "    pituitary/",
                "  Testing/",
                "    glioma/",
                "    meningioma/",
                "    notumor/",
                "    pituitary/",
            ]
        )
        missing_preview = "\n".join(f"- {path}" for path in missing_paths[:8])
        raise FileNotFoundError(
            "Brain MRI dataset is missing or has the wrong folder structure.\n"
            f"Expected:\n{expected_structure}\n\n"
            f"Missing paths:\n{missing_preview}"
        )


def build_loaders(dataset_root: Path, batch_size: int):
    validate_dataset_root(dataset_root)

    transform = transforms.Compose(
        [
            transforms.Resize((224, 224)),
            transforms.ToTensor(),
            transforms.Normalize([0.5, 0.5, 0.5], [0.5, 0.5, 0.5]),
        ]
    )

    train_data = datasets.ImageFolder(dataset_root / "Training", transform=transform)
    test_data = datasets.ImageFolder(dataset_root / "Testing", transform=transform)

    if train_data.classes != LABELS:
        raise ValueError(f"Expected classes {LABELS}, got {train_data.classes}")

    validation_size = max(1, int(len(train_data) * 0.15))
    train_size = len(train_data) - validation_size
    train_dataset, validation_dataset = random_split(
        train_data,
        [train_size, validation_size],
        generator=torch.Generator().manual_seed(42),
    )

    return (
        DataLoader(train_dataset, batch_size=batch_size, shuffle=True),
        DataLoader(validation_dataset, batch_size=batch_size, shuffle=False),
        DataLoader(test_data, batch_size=batch_size, shuffle=False),
    )


def build_model(device: torch.device):
    model = models.resnet50()
    model.fc = nn.Linear(model.fc.in_features, len(LABELS))
    return model.to(device)


def run_epoch(model, loader, criterion, optimizer, device):
    model.train()
    total_loss = 0.0
    correct = 0
    seen = 0

    for images, labels in loader:
        images = images.to(device)
        labels = labels.to(device)

        optimizer.zero_grad()
        outputs = model(images)
        loss = criterion(outputs, labels)
        loss.backward()
        optimizer.step()

        total_loss += loss.item() * images.size(0)
        correct += (outputs.argmax(1) == labels).sum().item()
        seen += images.size(0)

    return total_loss / seen, correct / seen


@torch.no_grad()
def evaluate(model, loader, criterion, device):
    model.eval()
    total_loss = 0.0
    correct = 0
    seen = 0

    for images, labels in loader:
        images = images.to(device)
        labels = labels.to(device)
        outputs = model(images)
        loss = criterion(outputs, labels)

        total_loss += loss.item() * images.size(0)
        correct += (outputs.argmax(1) == labels).sum().item()
        seen += images.size(0)

    return total_loss / seen, correct / seen


def export_onnx(model, output_path: Path, device: torch.device):
    model.eval()
    dummy_input = torch.randn(1, 3, 224, 224, device=device)
    torch.onnx.export(
        model,
        dummy_input,
        output_path,
        input_names=["input"],
        output_names=["logits"],
        dynamic_axes={"input": {0: "batch"}, "logits": {0: "batch"}},
        opset_version=17,
        dynamo=False,
    )


def main():
    parser = argparse.ArgumentParser(description="Train and export the brain MRI ResNet50 model.")
    parser.add_argument("--dataset", type=Path, default=DEFAULT_DATASET)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--epochs", type=int, default=10)
    parser.add_argument("--batch-size", type=int, default=32)
    parser.add_argument("--learning-rate", type=float, default=0.001)
    args = parser.parse_args()

    dataset_root = args.dataset.resolve()
    output_root = args.output.resolve()

    output_root.mkdir(parents=True, exist_ok=True)
    device = torch.device("cuda" if torch.cuda.is_available() else "cpu")
    train_loader, validation_loader, test_loader = build_loaders(dataset_root, args.batch_size)
    model = build_model(device)
    criterion = nn.CrossEntropyLoss()
    optimizer = optim.Adam(model.parameters(), lr=args.learning_rate)

    best_validation_accuracy = 0.0
    weights_path = output_root / "brain-tumor-resnet50.pth"
    onnx_path = output_root / "brain-tumor-resnet50.onnx"

    for epoch in range(1, args.epochs + 1):
        train_loss, train_accuracy = run_epoch(model, train_loader, criterion, optimizer, device)
        validation_loss, validation_accuracy = evaluate(model, validation_loader, criterion, device)
        print(
            f"epoch={epoch} "
            f"train_loss={train_loss:.4f} train_accuracy={train_accuracy:.4f} "
            f"validation_loss={validation_loss:.4f} validation_accuracy={validation_accuracy:.4f}"
        )

        if validation_accuracy >= best_validation_accuracy:
            best_validation_accuracy = validation_accuracy
            torch.save(model.state_dict(), weights_path)

    model.load_state_dict(torch.load(weights_path, map_location=device))
    test_loss, test_accuracy = evaluate(model, test_loader, criterion, device)
    export_onnx(model, onnx_path, device)

    metadata = {
        "labels": LABELS,
        "imageSize": 224,
        "normalization": {"mean": [0.5, 0.5, 0.5], "std": [0.5, 0.5, 0.5]},
        "testLoss": test_loss,
        "testAccuracy": test_accuracy,
        "onnxPath": str(onnx_path),
        "weightsPath": str(weights_path),
    }
    (output_root / "brain-tumor-resnet50.metadata.json").write_text(json.dumps(metadata, indent=2))
    print(json.dumps(metadata, indent=2))


if __name__ == "__main__":
    main()
