#!/bin/bash

# The path to the directory of this file
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

sudo docker run --rm -it --net=host --name r2wars -v $DIR/warriors:/opt/r2wars/csharp/bin/Release/warriors r2wars
