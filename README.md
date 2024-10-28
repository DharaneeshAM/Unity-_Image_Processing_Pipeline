# Image Processing Pipeline

# _Unity AI_

[![Made with Unity](https://img.shields.io/badge/Made%20with-Unity-57b9d3.svg?style=for-the-badge&logo=unity)](https://unity3d.com) ![C++](https://img.shields.io/badge/c++-%2300599C.svg?style=for-the-badge&logo=c%2B%2B&logoColor=white) ![OpenAI](https://img.shields.io/badge/OpenAI-%23000000.svg?style=for-the-badge&logo=openai&logoColor=white)

## Overview

This project processes book images to classify content as text, images, or both. Based on the classification, it uses:

- **YOLOv3** and Tesseract to determine the content type.
- **OCR (Tesseract)** to extract text when needed.
- **BLIP** model to retrieve descriptions or contextual information for images.
- **GPT** model or **Ollama** model to process combined outputs and provide insightful responses.

## Installation
Detailed installation steps for each, including commands and environment setup.

## Step 1: Set Up Virtual Environment : 
- Create and activate a virtual environment to manage dependencies cleanly.

      python -m venv image_processing_env

      source image_processing_env/bin/activate  # Linux/Mac

      image_processing_env\Scripts\activate     # Windows


## Step 2: Install YOLOv3 Dependencies
- YOLOv3 requires OpenCV and other dependencies for object detection.

      pip install opencv-python-headless numpy
You'll need the YOLOv3 weights, configuration files, and COCO class names:

- Download yolov3-tiny.weights and yolov3-tiny.cfg from the official YOLO website or GitHub repositories, and place them in your project directory.

## Step 3: Install Tesseract for OCR
- First, install the Tesseract executable (required for local use):

      Linux: sudo apt install tesseract-ocr

Then install the Tesseract Python wrapper using pip:

    pip install pytesseract

- Windows: Download the installer from Tesseract's GitHub and add Tesseract to your system PATH.

      pytesseract.pytesseract.tesseract_cmd = r'C:\Program Files\Tesseract-OCR\tesseract.exe'

## Step 4: Install BLIP Model Dependencies
Install the necessary dependencies for BLIP using Hugging Face's transformers and torch

    pip install torch transformers

Download the BLIP model using the Hugging Face API:

    from transformers import BlipProcessor, BlipForConditionalGeneration
    processor = BlipProcessor.from_pretrained("Salesforce/blip-image-captioning-large")
    model = BlipForConditionalGeneration.from_pretrained("Salesforce/blip-image-captioning-large")

## Step 5: Queries

- For ONLINE QUERIE response use OpenAI **(GPT Api Kye)**
  <img src="./Images/open ai.png" alt="View" width="110"/>
- Or, if you are using a OFFLINE hosted model like **Ollama**
  <img src="./Images/ollama.png" alt="View" width="130"/>

## Unity Setup (Required Packages):

To set up your Unity project for this image processing pipeline, ensure you have the following packages installed:

Unity Package Manager:

- **UnityWebRequest:** For web requests to the local server.
- **OpenAI:** For integrating OpenAI API calls.
- **Newtonsoft.Json:** For JSON serialization and deserialization.

## Unity Script

[Image Phase1 (For GPT Online Queries)](Unity%20Script/ImagePhase1.cs)

- This script handles image processing and sending queries to GPT's online API for response generation. Place this script in your Unity project for online processing.

[Image Phase2 (For Ollama Offline Queries)](Unity%20Script/Image%20Phase2.cs)

- This script handles image processing and sends queries to the local Ollama model. Use this in Unity for local processing when working with the Ollama setup.

## Model Prompt for GPT and Ollama 
    "I process book images using Tesseract for OCR text and the BLIP model for image-text retrieval. I'll provide outputs from both models and ask queries based on them. Please use the outputs to answer my questions. Give the answer only.

    BLIP Model Output = 
    OCR Model Output = 
    Query ="
