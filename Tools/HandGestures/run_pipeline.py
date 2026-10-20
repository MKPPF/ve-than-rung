"""CLI adapter. All ML source lives in HandGestureControl.ipynb definition cells."""
import argparse
import json
from pathlib import Path
from types import SimpleNamespace

ROOT = Path(__file__).resolve().parent


def load_pipeline():
    notebook = ROOT / "HandGestureControl.ipynb"
    document = json.loads(notebook.read_text(encoding="utf-8"))
    namespace = {"__name__": "hand_gesture_notebook", "__file__": str(notebook)}
    for cell in document["cells"]:
        if cell["cell_type"] == "code" and cell["metadata"].get("pipeline") == "definitions":
            exec(compile("".join(cell["source"]), f"{notebook}#{cell['id']}", "exec"), namespace)
    return SimpleNamespace(**namespace)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("action", choices=("prepare", "collect", "train", "play", "doctor"))
    parser.add_argument("--subject", default="p01")
    parser.add_argument("--split", choices=("train", "val", "test"), default="train")
    parser.add_argument("--hand", choices=("Left", "Right"), default="Right")
    parser.add_argument("--camera", type=int, default=0)
    parser.add_argument("--move-hand", choices=("Auto", "Left", "Right"), default="Auto")
    parser.add_argument("--movement-style", choices=("both", "point", "palm"), default="both")
    parser.add_argument("--dead-zone", type=float, default=0.08)
    parser.add_argument("--per-class", type=int, default=2000)
    parser.add_argument("--epochs", type=int, default=200)
    parser.add_argument("--data", nargs="+", type=Path)
    args = parser.parse_args()
    pipeline = load_pipeline()
    if args.action == "prepare":
        pipeline.prepare_data(per_class=args.per_class)
    elif args.action == "collect":
        pipeline.collect_data(subject=args.subject, split=args.split, hand=args.hand, camera=args.camera)
    elif args.action == "train":
        paths = args.data or [pipeline.HAGRID_PATH]
        if args.data is None and pipeline.WEBCAM_PATH.exists():
            paths.append(pipeline.WEBCAM_PATH)
        pipeline.train_model(paths, epochs=args.epochs)
    elif args.action == "play":
        pipeline.run_controller(camera=args.camera, move_hand=args.move_hand,
                                movement_style=args.movement_style, dead_zone=args.dead_zone)
    else:
        import numpy as np
        with pipeline.create_detector() as detector:
            hands = pipeline.detect(detector, np.zeros((256, 256, 3), dtype=np.uint8), 1)
            assert not hands
        print("Notebook definitions, MediaPipe model and video inference work (no camera opened).")


if __name__ == "__main__":
    main()
