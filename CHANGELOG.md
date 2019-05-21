# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## Unreleased

## v1.2.1 - 2019-05-20

### Changed
- mention set environment variables from .ok-cli-tool.env file

### Fixed
- display correct .env config file name (.ok-cli-tool.env NOT .ok-cli-tool)
- restore original color when Ctrl+C is pressed at prompt
- use '-c' instead of '/C' when calling bash

## v1.2.0 - 2019-04-18

### Added
- support for configuration via a .ok-cli-tool.env file
- pre & post build tasks to get and embed the git commit # 
- info option to display runtime and configuration details 
- roadmap to README.md

### Changed
- extracted command line options definition and parsing to CommandLineOptions class
- colorized help output
- reconsidered namespace s

## v1.1.0 - 2019-04-05

### Added
- prompt for command index when no arguments are specified
- -V flag for short form of --version
- project badges to README.md
- this CHANGELOG.md file

### Fixed
- exception being thrown for zero-length files

## v1.0.0 - 2019-03-29

- initial release
