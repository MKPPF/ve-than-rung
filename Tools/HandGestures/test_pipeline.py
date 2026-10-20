import csv
import contextlib
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import numpy as np

from run_pipeline import load_pipeline

pipeline = load_pipeline()
LABELS = pipeline.LABELS
GestureGate = pipeline.GestureGate
features = pipeline.features
movement_vector = pipeline.movement_vector
open_dataset = pipeline.open_dataset
read_datasets = pipeline.read_datasets


class FeatureTests(unittest.TestCase):
    def setUp(self):
        self.points = np.random.default_rng(10).uniform(0.2, 0.8, (21, 2))

    def test_translate_scale_and_mirror_invariance(self):
        base = features(self.points)
        np.testing.assert_allclose(base, features(self.points * 2 + 3), atol=2e-6)
        mirrored = self.points.copy()
        mirrored[:, 0] = 1 - mirrored[:, 0]
        np.testing.assert_allclose(base, features(mirrored), atol=2e-6)

    def test_invalid_hand_is_rejected(self):
        for points in (np.zeros((21, 2)), np.zeros((20, 2)), np.full((21, 2), np.nan)):
            with self.assertRaises(ValueError):
                features(points)


class ControllerTests(unittest.TestCase):
    def test_first_sign_and_changed_sign_fire_without_learned_neutral(self):
        gate = GestureGate()
        self.assertEqual(gate.update("fist", 1), -1)
        self.assertEqual(gate.update("fist", 1.2), -1)
        self.assertEqual(gate.update("fist", 1.4), 0)
        self.assertEqual(gate.update("fist", 2), -1)
        self.assertEqual(gate.update("peace", 4), -1)
        self.assertEqual(gate.update("peace", 4.4), 1)
        self.assertEqual(gate.update("peace", 8), -1)
        gate.update("like", 9)
        self.assertEqual(gate.update("like", 9.1), -1)
        self.assertEqual(gate.update("like", 10), 2)
        gate.update("none", 11)
        gate.update("none", 11.3)
        gate.update("like", 12)
        self.assertEqual(gate.update("like", 12.4), 2)

    def test_occlusion_and_uncertainty_do_not_repeat_same_spell(self):
        gate = GestureGate()
        gate.update("fist", 1)
        self.assertEqual(gate.update("fist", 1.4), 0)
        gate.update("unknown", 2)
        self.assertFalse(gate.armed)
        gate.update("fist", 3)
        self.assertEqual(gate.update("fist", 4), -1)
        gate.update("peace", 5)
        self.assertEqual(gate.update("peace", 5.4), 1)

    def test_jitter_never_counts_as_stable_new_sign_or_release(self):
        gate = GestureGate()
        for label, now in (("stop", 1), ("fist", 1.1), ("unknown", 1.2), ("stop", 1.3)):
            self.assertEqual(gate.update(label, now), -1)
        self.assertEqual(gate.update("stop", 1.7), 3)
        gate.update("none", 2)
        gate.update("stop", 2.1)
        self.assertEqual(gate.update("stop", 3), -1)

    def test_movement_clutch_direction_and_stop(self):
        center = [0.3, 0.5]
        cases = [([0.5, 0.5], [1, 0]), ([0.1, 0.5], [-1, 0]),
                 ([0.3, 0.2], [0, 1]), ([0.3, 0.8], [0, -1]), ([0.3, 0.5], [0, 0])]
        for wrist, expected in cases:
            np.testing.assert_array_equal(movement_vector("stop", 0.95, wrist, center), expected)
        for label, confidence in (("fist", 0.99), ("unknown", 0.99), ("stop", 0.2)):
            np.testing.assert_array_equal(movement_vector(label, confidence, [0.9, 0.9], center), [0, 0])


class MovementV2Tests(unittest.TestCase):
    @staticmethod
    def index_hand(angle=0):
        # Geometry fixtures exercise direction rules; these are never training data.
        xy = np.tile([0.5, 0.8], (21, 1)).astype(np.float32)
        xy[5], xy[6], xy[7], xy[8] = [0.44, 0.60], [0.44, 0.45], [0.44, 0.35], [0.44, 0.25]
        for mcp, pip, dip, tip, x in ((9, 10, 11, 12, 0.50), (13, 14, 15, 16, 0.56), (17, 18, 19, 20, 0.62)):
            xy[mcp], xy[pip], xy[dip], xy[tip] = [x, 0.62], [x, 0.50], [x, 0.60], [x, 0.70]
        rotation = np.array([[np.cos(angle), -np.sin(angle)], [np.sin(angle), np.cos(angle)]])
        return (xy - xy[0]) @ rotation.T + xy[0]

    @staticmethod
    def palm():
        xy = MovementV2Tests.index_hand()
        for mcp, pip, dip, tip in ((9, 10, 11, 12), (13, 14, 15, 16), (17, 18, 19, 20)):
            xy[pip, 1], xy[dip, 1], xy[tip, 1] = 0.48, 0.38, 0.28
        return xy

    def test_index_points_in_four_directions_without_palm_calibration(self):
        for angle, expected in ((0, [0, 1]), (np.pi/2, [1, 0]), (np.pi, [0, -1]), (-np.pi/2, [-1, 0])):
            motion = pipeline.HandMovementController()
            vector = motion.update({"Right": ("unknown", 0.3, self.index_hand(angle))}, 1)
            np.testing.assert_array_equal(vector, expected)
            self.assertEqual(motion.side, "Right")
            self.assertEqual(motion.cast_hand, "Left")

    def test_open_palm_and_invalid_landmarks_are_not_index_pointing(self):
        for xy in (self.palm(), np.zeros((21, 2)), np.full((21, 2), np.nan), np.zeros((20, 2))):
            np.testing.assert_array_equal(pipeline.pointing_vector(xy), [0, 0])

    def test_right_hand_auto_centers_and_moves_without_left_hand(self):
        motion = pipeline.HandMovementController()
        xy = self.palm()
        for now in (1, 1.3, 1.7):
            np.testing.assert_array_equal(motion.update({"Right": ("stop", 0.99, xy)}, now), [0, 0])
        self.assertIsNotNone(motion.center)
        shifted = xy + [0.2, 0]
        np.testing.assert_array_equal(motion.update({"Right": ("stop", 0.99, shifted)}, 2), [1, 0])
        np.testing.assert_array_equal(motion.update({"Right": ("fist", 0.99, shifted)}, 2.1), [0, 0])

    def test_lost_move_hand_does_not_turn_cast_hand_into_movement(self):
        motion = pipeline.HandMovementController()
        motion.update({"Right": ("unknown", 0.3, self.index_hand())}, 1)
        np.testing.assert_array_equal(motion.update({"Left": ("unknown", 0.3, self.index_hand())}, 2), [0, 0])
        self.assertEqual(motion.side, "Right")
        motion.reset(swap=True)
        np.testing.assert_array_equal(motion.update({"Left": ("unknown", 0.3, self.index_hand())}, 3), [0, 1])
        self.assertEqual(motion.cast_hand, "Right")

    def test_auto_center_requires_steady_confident_palm(self):
        motion = pipeline.HandMovementController(move_hand="Right", style="palm")
        xy = self.palm()
        motion.update({"Right": ("stop", 0.99, xy)}, 1)
        motion.update({"Right": ("stop", 0.99, xy + [0.1, 0])}, 1.5)
        motion.update({"Right": ("unknown", 0.4, xy)}, 1.8)
        self.assertIsNone(motion.center)
        self.assertFalse(motion.calibrate({"Right": ("fist", 0.99, xy)}))
        self.assertTrue(motion.calibrate({"Right": ("stop", 0.99, xy)}))

    def test_connection_feedback_distinguishes_focus_pause_and_disconnect(self):
        reply = dict(blocked=False, reason="")
        self.assertTrue(pipeline.unity_connection_message(reply, 1, 1.2)[1])
        self.assertFalse(pipeline.unity_connection_message(reply, 1, 3)[1])
        for reason, message in (("focus", "Game View"), ("paused", "unpause"), ("player", "gameplay")):
            text, ready = pipeline.unity_connection_message(dict(blocked=True, reason=reason), 1, 1.1)
            self.assertFalse(ready)
            self.assertIn(message, text)


class LiveCastingTests(unittest.TestCase):
    def run_frames(self, labels, times, release_frames=(), replies=True):
        """Exercise the full camera loop with simulated predictions and UDP ACKs."""
        import cv2

        frames = iter(enumerate(labels))
        current_label = [labels[0]]
        captured = []
        camera = unittest.mock.Mock()
        camera.isOpened.return_value = True
        camera.read.side_effect = lambda: (True, np.zeros((480, 640, 3), dtype=np.uint8))

        class Socket:
            pending = None
            def bind(self, endpoint): pass
            def setblocking(self, value): pass
            def close(self): pass
            def sendto(self, data, endpoint):
                packet = json.loads(data)
                captured.append(packet)
                self.pending = dict(version=1, kind="unity_status", session=packet["session"],
                                    blocked=False, reason="", focused=True, playerReady=True)
            def recvfrom(self, size):
                if not replies or self.pending is None:
                    raise BlockingIOError
                reply, self.pending = self.pending, None
                return json.dumps(reply).encode(), ("127.0.0.1", 5057)

        def detect(*args):
            index, current_label[0] = next(frames)
            xy = (MovementV2Tests.index_hand() if index in release_frames
                  else MovementV2Tests.palm())
            return [("Right", xy)]

        def predict(*args):
            label = current_label[0]
            values = np.full(len(LABELS), 0.001)
            if label == "unknown":
                values[:] = 0.2
            else:
                values[LABELS.index(label)] = 0.996
            return values

        with patch.object(cv2, "VideoCapture", return_value=camera), \
             patch.object(cv2, "imshow"), patch.object(cv2, "destroyAllWindows"), \
             patch.object(cv2, "waitKey", side_effect=[-1] * (len(labels)-1) + [27]), \
             patch.object(pipeline.run_controller.__globals__["socket"], "socket", return_value=Socket()), \
             patch.object(pipeline.run_controller.__globals__["time"], "monotonic", side_effect=times), \
             patch.dict(pipeline.run_controller.__globals__, {
                 "create_detector": lambda: contextlib.nullcontext(None),
                 "detect": detect, "predict": predict,
             }):
            pipeline.run_controller(loaded_model={}, move_hand="Left")
        camera.release.assert_called_once()
        events = {}
        for packet in captured:
            if packet["castId"]:
                events[packet["castId"]] = packet["skillSlot"]
        return list(events.values())

    def test_full_loop_first_spell_change_sign_and_index_release(self):
        labels = ["stop"]*4 + ["fist"]*2 + ["unknown", "fist", "fist", "unknown", "unknown", "fist", "fist"]
        times = [0, .1, .5, .6, .7, 1.1, 1.2, 1.3, 1.7, 1.8, 2.1, 2.2, 2.6]
        # First palm sends F; fist sends Q; loss cannot repeat Q; index releases Q.
        self.assertEqual(self.run_frames(labels, times, release_frames=(9, 10)), [3, 0, 0])

    def test_full_loop_no_casts_before_unity_ack(self):
        self.assertEqual(self.run_frames(["stop", "stop", "fist", "fist"], [0, .5, 1, 1.5], replies=False), [])


class DatasetTests(unittest.TestCase):
    def test_subject_leakage_rejected_before_training(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "test.csv"
            stream, writer = open_dataset(path)
            with stream:
                writer.writerow(["none", "same_subject", "train", "test", "a", *([0]*42)])
                writer.writerow(["fist", "same_subject", "test", "test", "b", *([0]*42)])
            with self.assertRaisesRegex(ValueError, "Subject leakage"):
                read_datasets([path])

    def test_duplicate_samples_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "test.csv"
            stream, writer = open_dataset(path)
            with stream:
                for _ in range(2):
                    writer.writerow(["none", "subject", "train", "test", "a", *([0]*42)])
            with self.assertRaisesRegex(ValueError, "Duplicate sample"):
                read_datasets([path])


if __name__ == "__main__":
    unittest.main()
