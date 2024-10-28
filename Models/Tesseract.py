from flask import Flask, request, jsonify
import pytesseract
from PIL import Image

# Create Flask app
app = Flask(__name__)

# Specify the path to the Tesseract executable
pytesseract.pytesseract.tesseract_cmd = r'C:\Program Files\Tesseract-OCR\tesseract.exe'

# Define route for image-to-text conversion
@app.route('/image-to-text', methods=['POST'])
def image_to_text():
    try:
        # Get image from request
        if 'image' not in request.files:
            return jsonify({'error': 'No image file found'}), 400

        image_file = request.files['image']
        
        # Open the image
        image = Image.open(image_file.stream)
        
        # Perform OCR using Tesseract
        extracted_text = pytesseract.image_to_string(image)

        # Return text as JSON response
        return jsonify({'text': extracted_text})
    
    except Exception as e:
        return jsonify({'error': str(e)}), 500

# Run the Flask server
if __name__ == '__main__':
    app.run(debug=True, host='0.0.0.0', port=5000)
