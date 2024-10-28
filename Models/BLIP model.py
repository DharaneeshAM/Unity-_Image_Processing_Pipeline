from flask import Flask, request, jsonify
from PIL import Image
import torch
from transformers import BlipProcessor, BlipForConditionalGeneration
import io

app = Flask(__name__)

# Load the BLIP processor and model
processor = BlipProcessor.from_pretrained("Salesforce/blip-image-captioning-base")
model = BlipForConditionalGeneration.from_pretrained("Salesforce/blip-image-captioning-base")

@app.route('/upload', methods=['POST'])
def upload_image():
    if 'image' not in request.files:
        return jsonify({"error": "No image part"}), 400


    file = request.files['image']
    if file.filename == '':
        return jsonify({"error": "No selected file"}), 400

    # Process the image
    image = Image.open(file.stream)
    image = processor(images=image, return_tensors="pt").pixel_values

    # Generate caption
    with torch.no_grad():
        outputs = model.generate(image)
        caption = processor.decode(outputs[0], skip_special_tokens=True)

    return jsonify({"caption": caption})

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=6000)
