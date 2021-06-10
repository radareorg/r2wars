#!/bin/bash

# The path to the directory of this file
DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" &> /dev/null && pwd )"

docker run --rm -it -p 9664:9664 -p 9966:9966 --name r2wars -v $DIR/warriors:/opt/r2wars/csharp/bin/Release/warriors r2wars
