import sys
import gridiron_gm_pkg

VERBOSE_SIM_OUTPUT = False

# Expose gridiron_gm_pkg as a submodule for legacy imports
sys.modules[__name__ + '.gridiron_gm_pkg'] = gridiron_gm_pkg
