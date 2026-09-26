"""Generate simple test assets for NyaAI.Examples.

Requires Pillow (images) and ffmpeg on PATH (audio/video).
Run from the repository root or the NyaAI.Examples directory.

    python NyaAI.Examples/tools/make-assets.py

Outputs into NyaAI.Examples/Assets:
    shape_red.png/.webp  — light background with a large red circle
    traffic_light.png    — dark background with a yellow circle
    tone_440.mp3         — steady 440 Hz tone, 2 s
    two_tones.wav        — 440 Hz then 880 Hz, 16 kHz mono
    clip_redbox.mp4      — blue background with a moving red square
"""

from __future__ import annotations

import shutil
import subprocess
from pathlib import Path

ASSETS = Path(__file__).resolve().parent.parent / "Assets"


def make_images() -> None:
    from PIL import Image, ImageDraw

    img = Image.new("RGB", (320, 180), (245, 245, 245))
    d = ImageDraw.Draw(img)
    d.ellipse([110, 30, 210, 130], fill=(200, 30, 30))
    img.save(ASSETS / "shape_red.png", "PNG")
    img.save(ASSETS / "shape_red.webp", "WEBP", quality=92)

    img2 = Image.new("RGB", (240, 320), (40, 40, 40))
    d2 = ImageDraw.Draw(img2)
    d2.ellipse([80, 20, 160, 100], outline=(255, 255, 255), width=3)
    d2.ellipse([80, 110, 160, 190], fill=(230, 200, 20))
    d2.ellipse([80, 200, 160, 280], outline=(255, 255, 255), width=3)
    img2.save(ASSETS / "traffic_light.png", "PNG")


def ffmpeg(*args: str) -> None:
    exe = shutil.which("ffmpeg")
    if exe is None:
        raise SystemExit("ffmpeg not found on PATH; skipping audio/video generation")
    subprocess.run([exe, "-v", "error", "-y", *args], check=True)


def make_media() -> None:
    ffmpeg("-f", "lavfi", "-i", "sine=frequency=440:duration=2",
           "-ac", "1", "-ar", "22050", str(ASSETS / "tone_440.mp3"))
    ffmpeg("-f", "lavfi", "-i",
           "sine=frequency=440:duration=1[a];sine=frequency=880:duration=1[b];"
           "[a][b]concat=n=2:v=0:a=1",
           "-ac", "1", "-ar", "16000", str(ASSETS / "two_tones.wav"))
    ffmpeg("-f", "lavfi", "-i",
           "color=c=blue:s=320x180:d=2:r=10,"
           "drawbox=x=100:y=40:w=120:h=100:color=red@1:t=fill",
           "-pix_fmt", "yuv420p", str(ASSETS / "clip_redbox.mp4"))


def main() -> None:
    ASSETS.mkdir(parents=True, exist_ok=True)
    make_images()
    make_media()
    for p in sorted(ASSETS.iterdir()):
        print(f"{p.name:22} {p.stat().st_size:>10,} bytes")


if __name__ == "__main__":
    main()
