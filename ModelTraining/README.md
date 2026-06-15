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

Use the Kaggle **Brain Tumor MRI Dataset** by Masoud Nickparvar:

```text
https://www.kaggle.com/datasets/masoudnickparvar/brain-tumor-mri-dataset/data
```

After downloading and extracting it, place the dataset under:

```text
mirai/datasets/brain-mri/
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
python ModelTraining/train_resnet50.py --epochs 10 --batch-size 4
```

By default, the script reads:

```text
mirai/datasets/brain-mri
```

and writes:

```text
backend/Models/brain-tumor-resnet50.pth
backend/Models/brain-tumor-resnet50.onnx
backend/Models/brain-tumor-resnet50.metadata.json
```

You can override the paths if needed:

```bash
python ModelTraining/train_resnet50.py --dataset path/to/dataset --output backend/Models --epochs 10 --batch-size 4
```

`--batch-size 4` is a safer default for Windows CPU training. If you train on a machine with more memory or a CUDA GPU, you can try a larger value such as `--batch-size 16` or `--batch-size 32`.
