"""
Demo the direct flying for the pymambo interface
"""
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

    print("taking off!")
    mambo.takeoff()

    # mambo.smart_sleep(5)
    #
    # mambo.land()
    return "Here"


@app.route("/land")
def land():
    print("landing")
    mambo.safe_land()
    print("disconnect")
    mambo.disconnect()
    return "Got Land request"


@app.route("/left")
def left():
    mambo.fly_direct(roll=25, pitch=0, yaw=0, vertical_movement=0, duration=0.5)
    print("Going left")
    return "Got Left request"


@app.route("/right")
def right():
    mambo.fly_direct(roll=-25, pitch=0, yaw=0, vertical_movement=0, duration=0.5)
    print("Going right")
    return "Got Right request"


@app.route("/lift")
def lift():
    mambo.fly_direct(roll=0, pitch=0, yaw=0, vertical_movement=25, duration=0.5)
    print("Going up")
    return "Got Lift request"


@app.route("/lower")
def lower():
    mambo.fly_direct(roll=0, pitch=0, yaw=0, vertical_movement=-25, duration=0.5)
    print("Going down")
    return "Got Lower request"


app.run(host="0.0.0.0")
