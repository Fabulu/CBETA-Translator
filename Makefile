# Simple Makefile for CBETA Translator Linux builds

.PHONY: build release run clean publish-debug publish-release publish-single-file install-deps help

# Default target
build:
	./eng/build-linux.sh Debug

# Release build (development)
release:
	./eng/build-linux.sh Release

# Run development build
run:
	./run-cbeta.sh

# Create self-contained debug build
publish-debug:
	./eng/build-linux.sh Debug true linux-x64

# Create self-contained release build
publish-release:
	./eng/build-linux.sh Release true linux-x64

# Advanced: create single-file self-contained build (can fail on some Linux setups)
publish-single-file:
	./eng/build-linux.sh Release true linux-x64 true

# Install dependencies
install-deps:
	@echo "Installing .NET SDK 8 (apt preferred for Linux self-contained publish)..."
	sudo apt update && sudo apt install -y dotnet-sdk-8.0

# Clean build artifacts
clean:
	dotnet clean ./CbetaTranslator.App.sln
	rm -rf ./publish/
	rm -rf ./bin/
	rm -rf ./obj/

# Show help
help:
	@echo "CBETA Translator Linux Build Commands:"
	@echo "  build               - Development build (Debug)"
	@echo "  release             - Development build (Release)"
	@echo "  run                 - Run development build"
	@echo "  publish-debug       - Create self-contained debug output"
	@echo "  publish-release     - Create self-contained release output"
	@echo "  publish-single-file - Create single-file self-contained output (advanced)"
	@echo "  install-deps        - Install .NET SDK 8 dependency"
	@echo "  clean               - Clean build artifacts"
	@echo "  help                - Show this help message"
