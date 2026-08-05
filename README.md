# **GnE – Gender-Name Estimator**

## **Overview**

Pronounced “Genie”, the Gender-Name Estimator uses the data from the
World Gender-Name Dictionary 2.0 to determine the likely gender of an
individual based upon their first name and country of origin.  

## Limitations, Cautions, Assumptions and License

- These results are only a launching point for future analysis and
  discussions and should be considered against other sources of data,
  e.g. internal HR records, and the like.

- We believe this tool to be useful and want to share it with others who
  may find it helpful. We do not provide assurance that the way we have
  built the tool will conform to your specific ways of tracking your
  inventor data, and we provide no guarantee of quality or accuracy.

- No claim of copyright is made in the supplied WIPO data lookup table.
  The upstream World Gender-Name Dictionary is distributed by WIPO under
  the MIT License: https://github.com/IES-platform/r4r_gender

## **License**

This project is licensed under the MIT License. See the LICENSE.md file for details.

## Citations

<dl>
  <dt>WIPO</dt>
  <dd>“Expanding the World Gender-Name Dictionary: WGND 2.0”, Martínez et al., WIPO Economics Research Working Paper No. 64<br/>
    <a href="https://www.wipo.int/publications/en/details.jsp?id=4554">https://www.wipo.int/publications/en/details.jsp?id=4554</a>
  </dd>
</dl>

## **Attribution**

GnE was created and is maintained by **Richardson Oliver LLP**.

## Building

GnE requires the .NET 10 SDK and the .NET macOS workload. The application is
published as a self-contained Apple-silicon build, so target Macs do not need a
separate .NET runtime installation.

EPPlus 8 must be licensed before spreadsheet processing. Set the
`EPPlusLicense` environment variable to `Commercial:<key>` for an authorized
commercial build. Richardson Oliver release builds instead generate the ignored
`appsettings.Secrets.json` file from `appsettings.Secrets.template.json` using
1Password; the key is never stored directly in this repository.

Richardson Oliver release maintainers can run
`scripts/build_and_notarize.zsh` to inject the licensed settings, build and sign
the application and installer, submit it for Apple notarization, staple the
ticket, validate Gatekeeper acceptance, and write the final package to
`artifacts/`.
