# Single-directional range sensor reading from an active ranger that emits energy
# and reports range reading in the downward direction (Z axis).
# This message is appropriate for altitude sensors used on drones
# for ground detection, landing assistance, and terrain following.
#
# The range value is measured from the sensor origin to the nearest obstacle
# in the downward direction.

float32 z_range         # range reading in the downward direction [m]

float32 min_range       # minimum range value for the sensor [m]
float32 max_range       # maximum range value for the sensor [m]

float32 field_of_view   # the size of the arc that the distance reading is
                        # valid for [rad]
                        # the object causing the range reading may have
                        # been anywhere within -field_of_view/2 and
                        # field_of_view/2 at the measured range.