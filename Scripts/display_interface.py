#!/usr/bin/python
# -*- coding:utf-8 -*-
import sys
import argparse
import os

parser = argparse.ArgumentParser()
parser.add_argument('--blacks', dest='blacks', type=str, help="Path to monochrome bitmap holding the image's black pixel content.")
parser.add_argument('--reds', dest='reds', type=str, help="Path to monochrome bitmap holding the image's red pixel content.")
parser.add_argument('--verbose', dest='is_debug', type=bool, action=argparse.BooleanOptionalAction, help="Verbose logging")
args = parser.parse_args()


picdir = os.path.join(os.path.dirname(os.path.realpath(__file__)), 'pic')
libdir = os.path.join(os.path.dirname(os.path.realpath(__file__)), 'lib')
if os.path.exists(libdir):
    sys.path.append(libdir)

import logging
from waveshare_epd import epd7in5bc
import time
from PIL import Image,ImageDraw,ImageFont
import traceback

if args.is_debug:
    logging.basicConfig(level=logging.DEBUG)

try:
    # Prep image files
    canvas_blacks_filename = args.blacks or './pic/7in5b-b.bmp'
    canvas_reds_filename = args.reds or './pic/7in5b-r.bmp'

    HBlackimage = Image.open(canvas_blacks_filename)
    HRYimage = Image.open(canvas_reds_filename)


    # Initialise comms w/ display
    epd = epd7in5bc.EPD()
    epd.init()
    # epd.Clear()
    
    # logging.info("3.read bmp file")
    # HBlackimage = Image.open(os.path.join(picdir, '7in5b-b.bmp'))
    # HRYimage = Image.open(os.path.join(picdir, '7in5b-r.bmp'))
    # HBlackimage = Image.open(os.path.join(picdir, '7in5c-b.bmp'))
    # HRYimage = Image.open(os.path.join(picdir, '7in5c-r.bmp'))

    # Write to display
    epd.display(epd.getbuffer(HBlackimage), epd.getbuffer(HRYimage))
    
    # logging.info("Clear...")
    # time.sleep(5)
    # epd.init()
    # epd.Clear()
    
    logging.info("Goto Sleep...")
    epd.sleep()
        
except IOError as e:
    logging.info(e)
    
except KeyboardInterrupt:    
    logging.info("ctrl + c:")
    epd7in5bc.epdconfig.module_exit(cleanup=True)
    exit()
