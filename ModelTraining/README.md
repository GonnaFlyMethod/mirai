# Brain MRI Model Training

The training script trains a four-class ResNet50 classifier with these labels:

- `glioma`
- `meningioma`
- `notumor`
- `pituitary`

The API loads the exported ONNX model from:

```text
backend/Models/brain-tumor-resnet50.onnx
```

## Dataset

Place the dataset under:

```text
datasets/brain-mri/
  Training/
    glioma/
    meningioma/
    notumor/
    pituitary/
  Testing/
    glioma/
    meningioma/
    notumor/
    pituitary/
```

Each class folder should contain MRI image files for that class.

## Train and Export

From the project root:

```bash
python ModelTraining/train_resnet50.py --epochs 10
```

By default, the script reads:

```text
datasets/brain-mri
```

and writes:

```text
backend/Models/brain-tumor-resnet50.pth
backend/Models/brain-tumor-resnet50.onnx
backend/Models/brain-tumor-resnet50.metadata.json
```

You can override the paths if needed:

```bash
python ModelTraining/train_resnet50.py --dataset path/to/dataset --output backend/Models --epochs 10
```
