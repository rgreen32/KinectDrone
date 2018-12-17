"""
Demo the direct flying for the pymambo interface
"""
from Mambo import Mambo
from flask import Flask
app = Flask(__name__)

# you will need to change this to the address of YOUR mambo
mamboAddr = "d0:3a:ac:00:e6:23"

# make my mambo object
mambo = Mambo(mamboAddr)
print("trying to connect")
success = mambo.connect(num_retries=3)
print("connected: %s" % success)


@app.route("/")
def start():

    # get the state information
    print("sleeping")
    mambo.smart_sleep(2)
    mambo.ask_for_state_update()
    mambo.smart_sleep(2)

    print("taking off!")
    mambo.safe_takeoff(5)

    # print("Flying direct: going forward (positive pitch)")
    # mambo.fly_direct(roll=0, pitch=50, yaw=0, vertical_movement=0, duration=1)
    #
    # print("Flying direct: yaw")
    # mambo.fly_direct(roll=0, pitch=0, yaw=50, vertical_movement=0, duration=1)
    #
    # print("Flying direct: going backwards (negative pitch)")
    # mambo.fly_direct(roll=0, pitch=-50, yaw=0, vertical_movement=0, duration=0.5)
    #
    # print("Flying direct: roll")
    # mambo.fly_direct(roll=50, pitch=0, yaw=0, vertical_movement=0, duration=1)
    #
    # print("Flying direct: going up")
    # mambo.fly_direct(roll=0, pitch=0, yaw=0, vertical_movement=50, duration=1)
    #
    # print("Flying direct: going around in a circle (yes you can mix roll, pitch, yaw in one command!)")
    # mambo.fly_direct(roll=25, pitch=0, yaw=50, vertical_movement=0, duration=5)

    print("landing")
    mambo.safe_land()
    mambo.smart_sleep(5)

    print("disconnect")
    mambo.disconnect()
    return "Here"


app.run(host="0.0.0.0")

