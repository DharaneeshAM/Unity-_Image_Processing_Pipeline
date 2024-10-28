from flask import Flask, request, jsonify
import cv2
import pytesseract
import os

app = Flask(__name__)

# Load YOLOv3-Tiny model using absolute paths
net = cv2.dnn.readNet("E:\\Image Project\\Phase 1\\.venv\\Scripts\\yolov3-tiny.weights", 
                      "E:\\Image Project\\Phase 1\\.venv\\Scripts\\yolov3-tiny.cfg")

# Get output layers
if cv2.__version__.startswith('4.'):
    output_layers = [net.getLayerNames()[i - 1] for i in net.getUnconnectedOutLayers()]
else:
    output_layers = [net.getLayerNames()[i[0] - 1] for i in net.getUnconnectedOutLayers()]

# Load COCO names for object detection
with open("E:\\Image Project\\Phase 1\\.venv\\Scripts\\coco.names", "r") as f:
    classes = [line.strip() for line in f.readlines()]

@app.route('/process-image', methods=['POST'])
def process_image():
    try:
        # Get the image file from the request
        image_file = request.files['image']
        
        # Save the image temporarily
        image_path = os.path.join('temp_image.jpg')
        image_file.save(image_path)

        # Load the image using OpenCV
        image = cv2.imread(image_path)
        height, width, _ = image.shape

        # Convert image to grayscale for better text detection
        gray_image = cv2.cvtColor(image, cv2.COLOR_BGR2GRAY)

        # Apply thresholding to preprocess the image for better OCR performance
        _, thresh_image = cv2.threshold(gray_image, 150, 255, cv2.THRESH_BINARY_INV)

        # Use Tesseract to detect text in the image
        text = pytesseract.image_to_string(thresh_image)

        # Object detection with YOLOv3-Tiny
        blob = cv2.dnn.blobFromImage(image, 0.00392, (416, 416), (0, 0, 0), True, crop=False)
        net.setInput(blob)
        outs = net.forward(output_layers)

        # Analyze the detections
        object_detected = False
        boxes = []
        confidences = []
        class_ids = []

        for out in outs:
            for detection in out:
                scores = detection[5:]
                class_id = int(scores.argmax())
                confidence = scores[class_id]

                # Lowered confidence threshold
                if confidence > 0.3:
                    object_detected = True
                    center_x = int(detection[0] * width)
                    center_y = int(detection[1] * height)
                    w = int(detection[2] * width)
                    h = int(detection[3] * height)
                    x = int(center_x - w / 2)
                    y = int(center_y - h / 2)

                    boxes.append([x, y, w, h])
                    confidences.append(float(confidence))
                    class_ids.append(class_id)

        # Use non-max suppression to eliminate multiple boxes for the same object
        indices = cv2.dnn.NMSBoxes(boxes, confidences, 0.3, 0.4)  # Lowered NMS thresholds

        # Collect all detected classes
        objects = [classes[class_ids[i]] for i in indices[0].flatten()] if len(indices) > 0 else []



        # Remove the temporary image file after processing
        os.remove(image_path)

        # Prepare the response based on detection results
        if text.strip() and object_detected:
            result = f"Text detected | Objects detected "
        elif text.strip():
            result = f"Text detected | No objects detected."
        elif object_detected:
            result = f"No text detected | Objects detected"
        else:
            result = "No text or No objects detected."

        return jsonify({"result": result})

    except Exception as e:
        return jsonify({"error": str(e)})

if __name__ == '__main__':
    # Run the server on localhost:4000
    app.run(host='0.0.0.0', port=4000)
