#!/usr/bin/env python3
import importlib.util
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
M = ROOT / "maintenance"
ARTIFACT = M / "non-iriya-v7-depth-regeneration-r82-canonical-argv-sequence-root.json"


def load_watchdog():
    spec = importlib.util.spec_from_file_location("cohort_watchdog", M / "cohort_checkpoint_watchdog.py")
    module = importlib.util.module_from_spec(spec)
    assert spec.loader
    spec.loader.exec_module(module)
    return module


def main() -> None:
    document = json.loads(ARTIFACT.read_text(encoding="utf-8"))
    commands = document["argv"]
    assert len(commands) == 4
    launch, research, config, constructor = commands
    assert any(item.endswith("launch_assigned_cohort.py") for item in launch)
    assert launch.count("cohort_checkpoint_watchdog.py") == 0
    assert research[1:3] == ["maintenance/cohort_checkpoint_watchdog.py", "research"]
    assert config == ["/usr/bin/python3.10", "maintenance/build_r82_config_b.py", "--cohort", "R82"]
    assert constructor[1:3] == ["maintenance/cohort_checkpoint_watchdog.py", "constructor"]
    assert constructor[-2:] == [
        "--authorized-wrapper-sha",
        document["authorizedHashes"]["wrapperSha256"],
    ]
    assert "--" not in constructor
    watchdog = load_watchdog()
    expected = watchdog.governed_constructor_command(
        (M / "dictionary_python_env.py").resolve(),
        (M / "generic_bounded_constructor.py").resolve(),
        (M / "non-iriya-v7-depth-regeneration-r82-constructor-config-b.json").resolve(),
        ROOT.resolve(),
    )
    assert expected == [
        str(Path("/usr/bin/python3.10").resolve()),
        str((M / "dictionary_python_env.py").resolve()),
        "--script",
        str((M / "generic_bounded_constructor.py").resolve()),
        "--",
        "--config",
        str((M / "non-iriya-v7-depth-regeneration-r82-constructor-config-b.json").resolve()),
        "--allowed-build-root",
        str(ROOT.resolve()),
    ]
    launcher = (M / "launch_assigned_cohort.py").read_text(encoding="utf-8")
    assert '"viability"' in launcher and "subprocess.run(command" in launcher
    print("PASS: R82 sequence has one launcher-owned viability step and a watchdog-owned constructor command")


if __name__ == "__main__":
    main()
