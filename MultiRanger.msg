# Multi-directional range sensor reading from an active ranger that emits energy
# and reports multiple range readings in different directions (front, back, left, right, up).
# This message is appropriate for multi-ranger sensors like those used on drones
# for obstacle avoidance and navigation.
#
# All range values are measured from the sensor origin to the nearest obstacle
# in each respective direction.

float32 front_range     # range reading in the front direction [m]
float32 back_range      # range reading in the back direction [m]
float32 left_range      # range reading in the left direction [m]
float32 right_range     # range reading in the right direction [m]
float32 up_range        # range reading in the up direction [m]

float32 min_range       # minimum range value for all sensors [m]
float32 max_range       # maximum range value for all sensors [m]

float32 field_of_view   # the size of the arc that each distance reading is
                        # valid for [rad]
                        # the object causing the range reading may have
                        # been anywhere within -field_of_view/2 and
                        # field_of_view/2 at the measured range.