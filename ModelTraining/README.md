# Brain MRI Model Training

The cloned reference project is in `External/brain-mri-analysis`. Its notebook trains a four-class ResNet50 classifier with these labels:

- `glioma`
- `meningioma`
- `notumor`
- `pituitary`

This project consumes an ONNX export from C# at `Models/brain-tumor-resnet50.onnx`.

## Dataset

Download the Kaggle dataset from the reference README and place it under:

```text
External/brain-mri-analysis/dataset
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

## Train and Export

Create the Conda environment from the cloned repo:

```bash
cd External/brain-mri-analysis
conda env create -f environment.yml
conda activate brain_mri
```

Then run the training/export helper from this project root:

```bash
python ModelTraining/train_resnet50.py --dataset External/brain-mri-analysis/dataset --output Models --epochs 10
```

The API will automatically load `Models/brain-tumor-resnet50.onnx` on the first scan analysis request.
