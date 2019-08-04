#!/bin/bash
set -ex
protoc -I./ -I./google -I/usr/local/include \
  --include_imports --include_source_info \
  --descriptor_set_out=./greet.pb ./*.proto