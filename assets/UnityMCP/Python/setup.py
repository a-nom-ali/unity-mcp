from setuptools import setup, find_packages

setup(
    name="unity-mcp",
    version="0.1.0",
    description="Unity integration through the Model Context Protocol",
    author="Your Name",
    author_email="your.email@example.com",
    packages=find_packages(),
    install_requires=[
        "mcp[cli]>=1.3.0",
    ],
    entry_points={
        "console_scripts": [
            "unity-mcp=unity_mcp_server:main",
        ],
    },
    classifiers=[
        "Programming Language :: Python :: 3",
        "License :: OSI Approved :: MIT License",
        "Operating System :: OS Independent",
    ],
    python_requires=">=3.10",
) 