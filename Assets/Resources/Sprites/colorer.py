from PIL import Image
import sys
import os

def make_pixels_white_preserve_alpha(image_path):
    with Image.open(image_path).convert("RGBA") as im:
        data = im.getdata()
        new_data = [(255, 255, 255, a) for (_, _, _, a) in data]
        im.putdata(new_data)
        im.save(image_path)

if __name__ == "__main__":
    if len(sys.argv) < 2:
        print("Usage: python script.py image1.png image2.png ...")
        sys.exit(1)

    for image_path in sys.argv[1:]:
        if not os.path.isfile(image_path):
            print(f"Skipping (not a file): {image_path}", file=sys.stderr)
            continue

        try:
            make_pixels_white_preserve_alpha(image_path)
            print(f"Processed: {image_path}")
        except Exception as e:
            print(f"Error processing {image_path}: {e}", file=sys.stderr)
