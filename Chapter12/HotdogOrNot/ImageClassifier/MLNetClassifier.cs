using Microsoft.ML.OnnxRuntime;
namespace HotdogOrNot.ImageClassifier;

internal class MLNetClassifier : IClassifier
{
    readonly InferenceSession session;
    readonly bool isBgr;
    readonly bool isRange255;
    readonly string inputName;
    readonly int inputSize;

    public MLNetClassifier(byte[] model) 
    {
        session = new InferenceSession(model);
        isBgr = session.ModelMetadata.CustomMetadataMap["Image.BitmapPixelFormat"] == "Bgr8";
        isRange255 = session.ModelMetadata.CustomMetadataMap["Image.NominalPixelRange"] == "NominalRange_0_255";
        inputName = session.InputMetadata.Keys.First();
        inputSize = session.InputMetadata[inputName].Dimensions[2];
    }

    public ClassifierOutput Classify(byte[] imageBytes)
    {
    }
}
